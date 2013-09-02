// Copyright 2013 Charlie Kindel
//   
//   

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Premise.Annotations;
using Premise.Services;
namespace Premise
{
    /// <summary>
    /// PremiseServer represents a Premise Server. It is a singleton class
    /// and thus only supports connection to a single server.
    /// 
    /// PremiseServer implements support for the Premise WebClient protocol
    /// spoken by the SYSConnector ActiveX control.
    /// </summary>
    public sealed class PremiseServer : IDisposable, INotifyPropertyChanged {
        // Singleton pattern (pattern #4) from John Skeet: 
        // http://csharpindepth.com/Articles/General/Singleton.aspx
        static PremiseServer() { }
        public static PremiseServer Instance{
            get { return instance; }
        }
        private static readonly PremiseServer instance = new PremiseServer();

        /// <summary>
        /// Each property subscription is managed in a dictionary of Subscription
        /// objects. Active is (currently) for debugging only. Subscriptions
        /// are inactive while the PremiseServer has suspsended the connection.
        /// All subscriptions are carried over a single TcpClient connection
        /// (because that's how premise sends property change notifciations).
        /// </summary>
        private class Subscription {
            public PremiseObject Object;
            public String PropertyName;
            public bool Active;
        }
        private Dictionary<int, Subscription> _subscriptions = new Dictionary<int, Subscription>();
        private TcpClient _subscriptionClient = new TcpClient();

        #region Delegates
        public delegate void GetPropertyCompletionMethod(DownloadStringCompletedEventArgs e);
        public delegate void InvokeMethodCompletionMethod(HttpResponseCompleteEventArgs e);
        #endregion

        private const Int32 TIMER_REQUERY_INTERVAL = 200; // 200 miliseconds

        public string Host, Username, Password;
        public int Port;
        public bool SSL;

        private bool _Connected = false;

        /// <summary>
        /// True if the subscription socket has successfully 
        /// recieved a valid response.
        /// False if the connection is closed.
        /// </summary>
        public bool Connected {
            get {
                return _Connected;
            }

            set {
                if (value == _Connected) return;
                _Connected = value;
                OnPropertyChanged("Connected");
            }
        }


        private bool _FastMode = false;
        /// <summary>
        /// Enable FastMode for the subscription socket.
        /// (Does not apply to Get/Set/Invoke actions).
        /// Setting this to False has no effect; close the 
        /// subscription connection and re-open it to disable
        /// FastMode.
        /// </summary>
        public bool FastMode
        {
            get {
                return _FastMode;
            }

            set {
                if (value == _FastMode) return;
                _FastMode = true;
                EnableFastMode();
                OnPropertyChanged("FastMode");
            }
        }

        /// <summary>
        /// StartSubscriptionsAsync must be called if you want subscription change notifications.
        /// This starts the subscription engine. We always create one subscription for
        /// Home DisplayName to start (but ignore any updates).
        /// </summary>
        public async Task StartSubscriptionsAsync(){
            await _subscriptionClient.ConnectAsync(Host, Port);

            Task.Run(() => ReadSubscriptionResponses());

            // We do a GetValue so we know we have a good connection
            SendRequest("sys://Home?f??" + "Name");

            if (FastMode) EnableFastMode();

            foreach (var subscription in _subscriptions) {
                SendSubscriptionRequest(subscription.Value);
            }

        }

        /// <summary>
        /// Stops the subscription engine. On the server, all subscritpions will be
        /// "forgotten". There's no need to actively unsubscribe them.
        /// </summary>
        public void StopSubscriptions() {
            foreach (var subscription in _subscriptions) {
                subscription.Value.Active = false;
            } 
            _subscriptionClient.Close();
        }

        /// <summary>
        /// Read from the TcpClient socket parsing responses. 
        /// </summary>
        private void ReadSubscriptionResponses()
        {
            // Process the response.
            StreamReader rdr = new StreamReader(_subscriptionClient.GetStream());
            int contentLength;
            int hashCode = 0;

            // From here on we will get responses on the stream that
            // look like HTTP POST responses.
            while (!rdr.EndOfStream) {
                var line = rdr.ReadLine();
                Debug.WriteLine(line);

                if (line.StartsWith("HTTP/1.1 404 ")) {
                    string error = line.Substring("HTTP/1.1 404 ".Length);
                    Debug.WriteLine("Error: " + error);
                    LastStatusCode = "404";
                    ParseErrorResponse();
                    continue;
                }
                LastStatusCode = "200";

                if (line.StartsWith("Target-Element: ")) {
                    string ID = line.Substring("Target-Element: ".Length);
                    if (!int.TryParse(ID, out hashCode)) {
                        Debug.WriteLine("Error: Target-Element: is not an integer.");
                    }
                }

                // Content-Length: is always the last HTTP header Premise sends
                if (!line.StartsWith("Content-Length: ") ||
                    !int.TryParse(line.Substring("Content-Length: ".Length), out contentLength)) continue;

                if (rdr.EndOfStream) continue;
                
                // Read the blank line that always follows Content-Length:
                line = rdr.ReadLine();
                Debug.WriteLine(line);

                if (rdr.EndOfStream) continue;

                // Read content
                char[] buffer = new char[contentLength];
                rdr.ReadBlock(buffer, 0, contentLength);
                line = new string(buffer);
                Debug.WriteLine(line);

                // See if this is our test GetValue
                // if hashCode == 0 then there was no Target-Element
                // and thus this content is not from a subscription.
                // Assume it is our response for Home 
                if (hashCode == 0 && line == "Home") {
                    Debug.WriteLine("Connected!");
                    Connected = true;
                    continue;
                }

                // Premise supports 32 connections. It uses pause/resumeConnection to 
                // enforce this limit. 
                switch (line) {
                    case "pauseConnection":
                        Console.WriteLine(line);
                        foreach (var subscription in _subscriptions) {
                            subscription.Value.Active = false;
                        }
                        break;
                    case "resumeConnection":
                        Console.WriteLine(line);
                        foreach (var subscription in _subscriptions) {
                            SendSubscriptionRequest(subscription.Value);
                        }
                        break;
                    case "fastMode":
                        FastMode = true;
                        Console.WriteLine(line);
                        break;
                    default:
                        // We got content!
                        // Find the property this response belongs to and update it
                        Subscription sub = null;
                        if (_subscriptions.TryGetValue(hashCode, out sub))
                            sub.Object.SetMember(sub.PropertyName, line, true);
                        break;
                }
            }
            Debug.WriteLine("Subscription socket closed.");
            Connected = false;
        }

        public string LastStatusCode;
        public string LastError;
        public string LastResponsePhrase;
        public string LastConnection;
        public string LastErrorContentType;
        public string LastErrorContent;

        private void ParseErrorResponse() {
            StreamReader rdr = new StreamReader(_subscriptionClient.GetStream());

            var line = rdr.ReadLine();
            Debug.WriteLine(line);
            while (!line.StartsWith("Content-Length: ")) {
                if (line.StartsWith("Connection: ")) 
                    LastConnection = line.Substring("Connection: ".Length);
                if (line.StartsWith("Content-Type: "))
                    LastErrorContentType = line.Substring("Content-Type: ".Length);
                if (line.StartsWith("Error: "))
                    LastError = line.Substring("Error: ".Length);
            }

            // Content-Length: is always the last HTTP header Premise sends
            int contentLength;
            if (!int.TryParse(line.Substring("Content-Length: ".Length), out contentLength)) return;

            if (rdr.EndOfStream) return;
            
            // Read the blank line that always follows Content-Length:
            line = rdr.ReadLine();
            Debug.WriteLine(line);

            if (rdr.EndOfStream) return;

            // Read content
            char[] buffer = new char[contentLength];
            rdr.ReadBlock(buffer, 0, contentLength);
            line = new string(buffer);
            LastErrorContent = line;
            Debug.WriteLine(line);
        }

        /// <summary>
        /// Send a subscription request to the server.
        /// </summary>
        /// <param name="po">The object with the property to subscribe.</param>
        /// <param name="propertyName">Name of the property to subscribe.</param>
        public void Subscribe(PremiseObject po, string propertyName) {
            Debug.Assert(_subscriptionClient.Connected);

            // Ignore multiple subscripitons
            foreach (var subscription in _subscriptions) {
                if (subscription.Value.PropertyName == propertyName &&
                    subscription.Value.Object.Location == po.Location) 
                    return;
            }

            Subscription sub = new Subscription { Object = po, PropertyName = propertyName };
            _subscriptions.Add(sub.GetHashCode(), sub);
            SendSubscriptionRequest(sub);
        }

        private void SendSubscriptionRequest(Subscription sub) {
            if (!String.IsNullOrEmpty(sub.Object.Location))
            {
                // We send as <object>?a?<hashcode>??<property>?<hashcode>?
                //
                // It may not be necessary to set the <subid> (2nd to last param)
                // but we do anyway.
                var command = String.Format("{0}?a?{1}??{2}?{3}?", 
                    sub.Object.Location, sub.GetHashCode(), sub.PropertyName, sub.GetHashCode());

                if (FastMode)
                    SendRequestFastMode(command, "");
                else {
                    SendRequest(command, "");
                }
                sub.Active = true;
            }
        }

        private void SendRequest(string command, string content = "") {
            var uri = new Uri(GetUrlFromSysUri(command));
            Debug.WriteLine("SendSubscriptionRequest: " + uri.PathAndQuery);

            string requestString = "POST " + uri.PathAndQuery + " HTTP/1.1\r\n";
            requestString += "User-Agent: PremiseServer .NET Client\r\n";
            requestString += "Host: " + Host + ":" + Port + "\r\n";
            requestString += "Connection: Keep-Alive\r\n";
            requestString += "SYSConnector: true\r\n\r\n";
            // Send the request.
            Debug.Assert(_subscriptionClient.Connected);
            StreamWriter writer = new StreamWriter(_subscriptionClient.GetStream());
            writer.Write(requestString);
            writer.Flush();            
        }

        private void SendRequestFastMode(string command, string content = "") {
            // fast mode uses a relaxed protocol that does not use HTTP headers
            // and command is not url encoded
            //
            // format is:
            //
            // <content-length><space><command><\r\n\r\n>[content, size_is(content-length)]
            // Content length is hex encoded
            if (command.StartsWith("sys://")) command = command.Remove(0, 6);
            string packet = content.Length.ToString("X8");
            packet += " ";
            packet += "/sys/" + command;
            packet += "\r\n\r\n";
            packet += content;
            Debug.WriteLine("SendRequestFastMode: " + packet);

            // Send the request.
            Debug.Assert(_subscriptionClient.Connected);
            StreamWriter writer = new StreamWriter(_subscriptionClient.GetStream());
            writer.Write(packet);
            writer.Flush();
        }

        private void EnableFastMode() {
            // /sys/{8D692EC9-EB74-4155-9D83-315872AC9800}?e?FastMode
            if (!_subscriptionClient.Connected) return;
            SendRequest("{8D692EC9-EB74-4155-9D83-315872AC9800}?e?FastMode", "True");
        }

        /// <summary>
        /// Send an unsubscribe request to the server.
        /// </summary>
        /// <param name="po"></param>
        /// <param name="propertyName"></param>
        public void Unsubscribe(PremiseObject po, string propertyName) {
            // Find subscription 
            foreach (var subscription in _subscriptions) {
                if (subscription.Value.PropertyName == propertyName &&
                    subscription.Value.Object.Location == po.Location) {
                    WebClient webclient = new WebClient();
                    webclient.Credentials = new NetworkCredential(Username, Password);
                    var uri = new Uri(GetUrlFromSysUri(po.Location) + "?c?" + subscription.GetHashCode());
                    Debug.WriteLine("Unsubscribe: " + uri);
                    webclient.UploadString(uri, "");
                }
            }
        }

        /// <summary>
        ///  Send a SetValue request to the sever
        ///   (e.g. url = http://localhost/sys/Home/Kitchen/Overheadlight?e?PowerState, message contains
        ///   "0" or "1"), no response message-body
        /// </summary>
        public void SetValue(string location, string property, string value)
        {
            WebClient webclient = new WebClient();
            webclient.Credentials = new NetworkCredential(Username, Password);
            var uri = new Uri(GetUrlFromSysUri(location) + "?e?" + property);
            Debug.WriteLine("SetValue: " + uri + ": " + value);
            webclient.UploadString(uri, value);
        }

        /// <summary>
        ///  Send a GetValue request to the sever
        ///   f - GetValue (e.g. url = http://localhost/sys/Home/Kitchen/Overheadlight?f??PowerState (Note double ??)
        ///   no message body in request
        ///   You *can* put a targetelement between the double ??, but not really necessary here.
        ///   response message body contains value
        /// </summary>
        public async Task<string> GetValueTaskAsync(string location, string property)
        {
            WebClient webclient = new WebClient();
            webclient.Credentials = new NetworkCredential(Username, Password);
            var uri = new Uri(GetUrlFromSysUri(location) + "!" + property);
            Debug.WriteLine("GetValueTaskAsync: " + uri);
            return await webclient.DownloadStringTaskAsync(uri);
        }

        /// <summary>
        ///  Send a GetValue request to the sever
        /// </summary>
        /// <param name="location"></param>
        /// <param name="property"></param>
        /// <param name="cm"></param>
        public void GetValueAsync(string location, string property, DownloadStringCompletedEventHandler cm)
        {
            WebClient webclient = new WebClient();
            webclient.Credentials = new NetworkCredential(Username, Password);
            var uri = new Uri(GetUrlFromSysUri(location) + "?f??" + property);
            Debug.WriteLine("GetValueAsync: " + uri);
            webclient.DownloadStringCompleted += cm;
            webclient.DownloadStringAsync(uri);
        }

        /// <summary>
        ///  Send a InvokeMethod request to the sever
        /// </summary>
        public async Task<string> InvokeMethodTaskAsync(string location, string method)
        {
            // <object>?d?<targetelementid>?[64]<method>
            // TODO: base64 encode method
            WebClient webclient = new WebClient();
            webclient.Credentials = new NetworkCredential(Username, Password);
            var uri = new Uri(GetUrlFromSysUri(location) + "?d??" + method);
            Debug.WriteLine("InvokeMethodTaskAsync: " + uri);
            return await webclient.DownloadStringTaskAsync(uri);
        }

        /// <summary>
        ///   Utility function to convert a sys URI (sys://Home/...) to an 
        ///   HTTP URL (http://server/sys/Home/...)
        /// </summary>
        public string GetUrlFromSysUri(string SysUrl)
        {
            if (SysUrl.StartsWith("sys://")) SysUrl = SysUrl.Remove(0, 6);
            Uri uriServer = SSL ? new UriBuilder("https", Host, Port, "sys/").Uri : new UriBuilder("http", Host, Port, "sys/").Uri;
            return uriServer.AbsoluteUri + SysUrl;
        }

        #region IDisposable Members

        public void Dispose() {
            _subscriptionClient.Close();
            _subscriptionClient = null;
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}