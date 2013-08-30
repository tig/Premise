// Copyright 2012 Charlie Kindel
//   
// This file is part of PremiseWP7
//   

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Premise.Services;

namespace pr
{
    /// <summary>
    ///   PremiseServer is the model for the Premise server. It handles all sets and gets from the 
    ///   SOAP interface exposed by Premise. It relies heavily on HttpHelper.
    ///   SYS URL format.
    ///   a - SubscribeToProperty
    ///   http://localhost/sys/Home?a?targetelementid??Brightness?subid?subname
    ///   b - SubscribeToClass
    ///   http://localhost/sys/object?b?targetelementid?action?class?subscription_id?subscription_name
    ///   Class subscriptions don't get prop change events, just add/delete
    ///   c - Unsubscribe
    ///   d - InvokeMethod (e.g. url = http://localhost/sys/Home?d??TestFn(True, "Gold").
    ///   * Works with script functions.  Use method.TestFn, method.ParamName1, etc...
    ///   * Does not work with script methods.
    ///   * Does not appear to work with built-in object methods (e.g. IsOfExplicitType(type) fails).
    ///   e - SetValue (e.g. url = http://localhost/sys/Home/Kitchen/Overheadlight?e?PowerState, message contains
    ///   "0" or "1"), no response message-body
    ///   f - GetValue (e.g. url = http://localhost/sys/Home/Kitchen/Overheadlight?f??PowerState (Note double ??)
    ///   no message body in request
    ///   You *can* put a targetelement between the double ??, but not really necessary here.
    ///   response message body contains value
    ///   g - RunCommand
    /// </summary>
    public sealed class PremiseServer
    {
        // Singleton pattern (pattern #4) from John Skeet: http://csharpindepth.com/Articles/General/Singleton.aspx

        #region Delegates

        public delegate void ConnectCompleteEventHandler(object sender, ConnectCompleteEventArgs e);

        public delegate void GetPropertyCompletionMethod(HttpResponseCompleteEventArgs e);

        public delegate void InvokeMethodCompletionMethod(HttpResponseCompleteEventArgs e);

        #endregion

        private const Int32 TIMER_REQUERY_INTERVAL = 200; // 200 miliseconds

        private static readonly PremiseServer instance = new PremiseServer();

        static PremiseServer()
        {
        }

        private PremiseServer()
        {
        }

        public static PremiseServer Instance
        {
            get { return instance; }
        }

        public string Host, Username, Password;
        public int Port;
        public bool SSL;

        /// <summary>
        ///   Converts a sys URI (sys://Home/...) to an HTTP URL (http://server/sys/Home/...)
        /// </summary>
        public string GetUrlFromSysUri(string SysUrl)
        {
            // Remove the "sys:/" (5 characters) and replace with the base server Uri
            Uri uriServer;
            uriServer = SSL ? new UriBuilder("https", Host, Port, "sys").Uri : new UriBuilder("http", Host, Port, "sys").Uri;

            return uriServer.AbsoluteUri + SysUrl.Remove(0, 5);
        }

        /// <summary>
        ///   e - SetValue (e.g. url = http://localhost/sys/Home/Kitchen/Overheadlight?e?PowerState, message contains
        ///   "0" or "1"), no response message-body
        /// </summary>
        public void SetPropertyAsync(string location, string property, string value)
        {
            var uri = new Uri(GetUrlFromSysUri(location) + "?e?" + property);
            Debug.WriteLine("SetPropertyAsync: url = <" + uri + "> value=" + value);
            var helper = new HttpHelper(uri, "POST", value, false, property, Username, Password);
            helper.Execute();
        }

        /// <summary>
        ///   f - GetValue (e.g. url = http://localhost/sys/Home/Kitchen/Overheadlight?f??PowerState (Note double ??)
        ///   no message body in request
        ///   You *can* put a targetelement between the double ??, but not really necessary here.
        ///   response message body contains value
        /// </summary>
        public Task<string> GetPropertyAsync<T>(string location, T property)
        {
            var uri = new Uri(GetUrlFromSysUri(location) + "?f??" + property);
            var helper = new HttpHelper(uri, "POST", null, true, property, Username, Password);

            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
            //Debug.WriteLine("GetPropertyAsync: url = <" + uri.ToString() + ">");
            helper.ResponseComplete += args => {
                try {
                    tcs.SetResult(args.Response);
                }
                catch(Exception exc) { tcs.SetException(exc);}
            };
            helper.Execute();
            return tcs.Task;
        }

        /// <summary>
        ///   d - InvokeMethod
        /// </summary>
        public void InvokeMethodAsync<T>(string location, T method, GetPropertyCompletionMethod cm,
                                         params object[] parameters)
        {
            var uri = new Uri(GetUrlFromSysUri(location) + "?d??" + method);
            var helper = new HttpHelper(uri, "POST", null /*parameters.ToString()*/, true, method, Username, Password);
            //Debug.WriteLine("GetPropertyAsync: url = <" + uri.ToString() + ">");
            helper.ResponseComplete += new HttpResponseCompleteEventHandler(cm);
            helper.Execute();
        }

        /// <summary>
        ///   Connect simply attempts to retrieve the DisplayName property fo the Home object. It fires
        ///   the ConnectComplete event when completed.  eSucceeded is True if DisplayName was recieved
        /// </summary>
        public async Task<string> Connect()
        {
            Debug.WriteLine("PremiseServer.Connect: {0}", GetUrlFromSysUri("sys://Home"));
            return await GetPropertyAsync("sys://Home", "DisplayName");
        }

        #region Nested type: ConnectCompleteEventArgs

        public class ConnectCompleteEventArgs : EventArgs
        {
            public string Response;
            public bool Succeeded;

            public ConnectCompleteEventArgs(string response, bool succeeded)
            {
                Response = response;
                this.Succeeded = succeeded;
            }
        }

        #endregion
    }
}