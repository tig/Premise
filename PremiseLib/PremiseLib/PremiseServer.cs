// Copyright 2013 Kindel Systems
//   

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using PremiseLib.Annotations;

namespace PremiseLib {
    /// <summary>
    /// PremiseServer represents a Premise Server. It is a singleton class
    /// and thus only supports connection to a single server.
    /// 
    /// PremiseServer implements support for the Premise WebClient protocol
    /// spoken by the SYSConnector ActiveX control.
    /// </summary>
    public sealed class PremiseServer : IPremiseNotify, INotifyPropertyChanged, IDisposable {
        // Singleton pattern (pattern #4) from John Skeet: 
        // http://csharpindepth.com/Articles/General/Singleton.aspx
        static PremiseServer() {
        }

        public static PremiseServer Instance {
            get { return instance; }
        }

        private static readonly PremiseServer instance = new PremiseServer();

        // Each .NET client has a different way of dispatching events to the
        // 'ui thread'. We isolate these in an IPremiseNotify implementation
        // that the caller to PremiseServer can give us. The default 
        // implementation does no dispatching.
        private IPremiseNotify _notifier;
        public IPremiseNotify Notifier {
            get {
                if (_notifier == null) 
                    _notifier = this;
                return _notifier;
            }
            set {
                _notifier = value;
            }
        }

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
        private IPremiseSocket _subscriptionClient = null;

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
            get { return _Connected; }

            set {
                if (value == _Connected) return;
                _Connected = value;
                Notifier.OnPropertyChanged(this, PropertyChanged);
            }
        }

        private bool _error = false;

        public bool Error {
            get { return _error; }

            set {
                if (value == _error) return;
                _error = value;
                Notifier.OnPropertyChanged(this, PropertyChanged);
            }
        }

        public string LastStatusCode;
        public string LastError;
        public string LastResponsePhrase;
        public string LastConnection;
        public string LastErrorContentType;
        public string LastErrorContent;

        private bool _FastMode = false;

        /// <summary>
        /// Enable FastMode for the subscription socket.
        /// (Does not apply to Get/Set/Invoke actions).
        /// Setting this to False has no effect; close the 
        /// subscription connection and re-open it to disable
        /// FastMode.
        /// </summary>
        public bool FastMode {
            get { return _FastMode; }

            set {
                if (value == _FastMode) return;
                _FastMode = true;
                EnableFastMode();
                Notifier.OnPropertyChanged(this, PropertyChanged);
            }
        }

        /// <summary>
        /// StartSubscriptionsAsync must be called if you want subscription change notifications.
        /// This starts the subscription engine. We always create one subscription for
        /// Home DisplayName to start (but ignore any updates).
        /// </summary>
        public async Task StartSubscriptionsAsync(IPremiseSocket premiseSocket) {
            try {
                _subscriptionClient = premiseSocket;
                await _subscriptionClient.ConnectAsync(Host, Port, SSL, Username, Password);

                // Assign the resulting task to a local variable to get around
                // the compiler warning about not awaiting this.
                // http://stackoverflow.com/questions/18577054/alternative-to-task-run-that-doesnt-throw-warning
                Task task = Task.Run(() => ReadSubscriptionResponses());

                // We do a GetValue so we know we have a good connection
                if (await SendRequest("sys://Home?f??" + "Name")) {

                    if (FastMode) EnableFastMode();

                    foreach (var subscription in _subscriptions) {
                        Task t = SendSubscriptionRequest(subscription.Value);
                    }
                }
                else {
                    Debug.WriteLine("SendRequest returned false");
                }
            }
            catch (Exception ex) {
                Debug.WriteLine(ex.ToString());
                Dispose();
                throw ex;
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
            Dispose();
        }

        /// <summary>
        /// Read from the TcpClient socket parsing responses. 
        /// </summary>
        /// <summary>
        /// Read from the TcpClient socket parsing responses. 
        /// </summary>
        private async void ReadSubscriptionResponses() {
            try {
                // Process the response.
                int contentLength;
                int hashCode = 0;

                // From here on we will get responses on the stream that
                // look like HTTP POST responses.
                while (_subscriptionClient != null) {
                    var line = await _subscriptionClient.ReadLineAsync();
                    Debug.Assert(line != null);
                    Debug.WriteLine(line);

                    if (line.StartsWith("HTTP/1.1 ")) {
                        string statusCode = line.Substring("HTTP/1.1 ".Length, 3);
                        LastStatusCode = statusCode;
                        LastError = line.Substring("HTTP/1.1 ".Length + 4);
                        if (!statusCode.StartsWith("2")) {
                            Debug.WriteLine("Error: " + line);
                            await ParseErrorResponse();
                            Dispose();
                            Error = true;
                            break;
                        }
                    }

                    if (line.StartsWith("Target-Element: ")) {
                        string ID = line.Substring("Target-Element: ".Length);
                        if (!int.TryParse(ID, out hashCode)) {
                            Debug.WriteLine("Error: Target-Element: is not an integer.");
                        }
                    }

                    // Content-Length: is always the last HTTP header Premise sends
                    if (!line.StartsWith("Content-Length: ") ||
                        !int.TryParse(line.Substring("Content-Length: ".Length), out contentLength)) continue;

                    if (_subscriptionClient == null) continue;

                    // Read the blank line that always follows Content-Length:
                    line = await _subscriptionClient.ReadLineAsync();
                    Debug.WriteLine(line);

                    if (_subscriptionClient == null) continue;

                    // Read content
                    line = await _subscriptionClient.ReadBlockAsync(contentLength);
                    Debug.WriteLine(line);

                    // See if this is our test GetValue
                    // if hashCode == 0 then there was no Target-Element
                    // and thus this content is not from a subscription.
                    // Assume it is our response for Home 
                    if (hashCode == 0 && line == "Home") {
                        Connected = true;
                        continue;
                    }

                    // Premise supports 32 connections. It uses pause/resumeConnection to 
                    // enforce this limit. 
                    switch (line) {
                        case "pauseConnection":
                            Debug.WriteLine(line);
                            foreach (var subscription in _subscriptions) {
                                subscription.Value.Active = false;
                            }
                            break;
                        case "resumeConnection":
                            Debug.WriteLine(line);
                            foreach (var subscription in _subscriptions) {
                                Task t = SendSubscriptionRequest(subscription.Value);
                            }
                            break;
                        case "fastMode":
                            FastMode = true;
                            Debug.WriteLine(line);
                            break;
                        default:
                            // We got content!
                            // Find the property this response belongs to and update it
                            Subscription sub = null;
                            if (_subscriptions.TryGetValue(hashCode, out sub))
                                Notifier.DispatchSetMember(sub.Object, sub.PropertyName, line);
                            break;
                    }
                }
            }
            catch (Exception ex) {
                Debug.WriteLine("ReadSubscriptionResponse: " + ex.ToString());
                Dispose();
                throw ex;
            }
            Debug.WriteLine("Subscription socket closed.");
            Connected = false;
            Dispose();
        }

        private async Task ParseErrorResponse() {
            try {
                int contentLength = 0;
                while (_subscriptionClient != null) {
                    var line = await _subscriptionClient.ReadLineAsync();
                    Debug.Assert(line != null);
                    Debug.WriteLine(line);

                    if (line.StartsWith("Connection: "))
                        LastConnection = line.Substring("Connection: ".Length);
                    if (line.StartsWith("Content-Type: "))
                        LastErrorContentType = line.Substring("Content-Type: ".Length);
                    if (line.StartsWith("Error: "))
                        LastError = line.Substring("Error: ".Length);

                    if (line.StartsWith("Content-Length: ")) {
                        // Content-Length: is always the last HTTP header Premise sends
                        if (!int.TryParse(line.Substring("Content-Length: ".Length), out contentLength)) return;
                    }

                    if (line.Length == 0) {
                        // The blank line that always preceeds content
                        //if (_subscriptionClient == null) return;
                        //line = await _subscriptionClient.ReadLineAsync();
                        //Debug.Assert(line.Length == 0);
                        //Debug.WriteLine(line);

                        // Read content
                        if (_subscriptionClient == null) return;
                        line = await _subscriptionClient.ReadBlockAsync(contentLength);
                        LastErrorContent = line;
                        Debug.WriteLine(line);
                        return;
                    }
                }
            }
            catch (Exception ex) {
                Dispose();
                throw ex;
            }
        }

        /// <summary>
        /// Send a subscription request to the server.
        /// </summary>
        /// <param name="po">The object with the property to subscribe.</param>
        /// <param name="propertyName">Name of the property to subscribe.</param>
        public async Task Subscribe(PremiseObject po, string propertyName) {
            try {
                Debug.Assert(_subscriptionClient != null);

                // Ignore multiple subscripitons
                foreach (var subscription in _subscriptions) {
                    if (subscription.Value.PropertyName == propertyName &&
                        subscription.Value.Object.Location == po.Location)
                        return;
                }

                Subscription sub = new Subscription {Object = po, PropertyName = propertyName};
                _subscriptions.Add(sub.GetHashCode(), sub);
                await SendSubscriptionRequest(sub);

            }
            catch (Exception ex) {
                Dispose();
                throw ex;
            }
        }

        private async Task SendSubscriptionRequest(Subscription sub) {
            try {
                if (String.IsNullOrEmpty(sub.Object.Location)) return;
                // We send as <object>?a?<hashcode>??<property>?<hashcode>?
                //
                // It may not be necessary to set the <subid> (2nd to last param)
                // but we do anyway.
                var command = String.Format("{0}?a?{1}??{2}?{3}?",
                                            sub.Object.Location, sub.GetHashCode(), sub.PropertyName,
                                            sub.GetHashCode());

                if (FastMode)
                    await SendRequestFastMode(command, "");
                else {
                    await SendRequest(command, "");
                }
                sub.Active = true;
            }
            catch (Exception ex) {
                Dispose();
                throw ex;
            }
        }

        private async Task<Boolean> SendRequest(string command, string content = "") {
            try {
                var uri = new Uri(GetUrlFromSysUri(command));
                Debug.WriteLine("SendSubscriptionRequest: " + uri.PathAndQuery);

                string requestString = "POST " + uri.PathAndQuery + " HTTP/1.1\r\n";
                requestString += "User-Agent: PremiseServer .NET Client\r\n";
                requestString += "Host: " + Host + ":" + Port + "\r\n";
                requestString += "Connection: Keep-Alive\r\n";
                requestString += "SYSConnector: true\r\n\r\n";
                // Send the request.
                Debug.Assert(_subscriptionClient != null);
                bool b = false;
                try {
                    b = await _subscriptionClient.WriteStringAsync(requestString);
                }
                catch (Exception ex) {
                    Debug.WriteLine(ex.ToString());
                }
                return b;
            }
            catch (Exception ex) {
                Dispose();
                throw ex;
            }
        }

        private async Task SendRequestFastMode(string command, string content = "") {
            // fast mode uses a relaxed protocol that does not use HTTP headers
            // and command is not url encoded
            //
            // format is:
            //
            // <content-length><space><command><\r\n\r\n>[content, size_is(content-length)]
            // Content length is hex encoded
            try {
                if (command.StartsWith("sys://")) command = command.Remove(0, 6);
                string packet = content.Length.ToString("X8");
                packet += " ";
                packet += "/sys/" + command;
                packet += "\r\n\r\n";
                packet += content;
                Debug.WriteLine("SendRequestFastMode: " + packet);

                // Send the request.
                Debug.Assert(_subscriptionClient != null);
                await _subscriptionClient.WriteStringAsync(packet);
            }
            catch (Exception ex) {
                Dispose();
                throw ex;
            }
        }

        private void EnableFastMode() {
            // /sys/{8D692EC9-EB74-4155-9D83-315872AC9800}?e?FastMode
            if (_subscriptionClient == null) return;
            Task t = SendRequest("{8D692EC9-EB74-4155-9D83-315872AC9800}?e?FastMode", "True");
        }

        /// <summary>
        /// Send an unsubscribe request to the server.
        /// </summary>
        /// <param name="po"></param>
        /// <param name="propertyName"></param>
        public async Task Unsubscribe(PremiseObject po, string propertyName) {
            try {
                // Find subscription 
                foreach (var subscription in _subscriptions) {
                    if (subscription.Value.PropertyName == propertyName &&
                        subscription.Value.Object.Location == po.Location) {
                        _subscriptions.Remove(subscription.Key);
                        string command = "?c?" + subscription.Key;
                        if (FastMode)
                            await SendRequestFastMode(command);
                        else {
                            await SendRequest(command);
                        }
                        return;
                    }
                }
            }
            catch (Exception ex) {
                Dispose();
                throw ex;
            }
        }

        //============ HttpClient based methods ==================

        /// <summary>
        ///  Send a SetValue request to the sever
        ///   (e.g. url = http://localhost/sys/Home/Kitchen/Overheadlight?e?PowerState, message contains
        ///   "0" or "1"), no response message-body
        /// </summary>
        public void SetValue(string location, string property, string value) {
            HttpClient webclient =
                new HttpClient(new HttpClientHandler() {Credentials = new NetworkCredential(Username, Password)});
            var uri = new Uri(GetUrlFromSysUri(location) + "?e?" + property);
            Debug.WriteLine("SetValue: " + uri + ": " + value);
            webclient.PostAsync(uri, new StringContent(value));

        }

        /// <summary>
        ///  Send a GetValue request to the sever
        ///   f - GetValue (e.g. url = http://localhost/sys/Home/Kitchen/Overheadlight?f??PowerState (Note double ??)
        ///   no message body in request
        ///   You *can* put a targetelement between the double ??, but not really necessary here.
        ///   response message body contains value
        /// </summary>
        public async Task<string> GetValueTaskAsync(string location, string property) {
            try {
                HttpClient webclient =
                    new HttpClient(new HttpClientHandler() {Credentials = new NetworkCredential(Username, Password)});
                var uri = new Uri(GetUrlFromSysUri(location) + "!" + property);
                Debug.WriteLine("GetValueTaskAsync: " + uri);
                return await webclient.GetStringAsync(uri);
            }
            catch (Exception ex) {
                throw ex;
            }
        }

        /// <summary>
        ///  Send a GetValue request to the sever
        /// </summary>
        /// <param name="location"></param>
        /// <param name="property"></param>
        /// <param name="cm"></param>
        //public void GetValueAsync(string location, string property, DownloadStringCompletedEventHandler cm)
        //{
        //    WebClient webclient = new WebClient();
        //    webclient.Credentials = new NetworkCredential(Username, Password);
        //    var uri = new Uri(GetUrlFromSysUri(location) + "?f??" + property);
        //    Debug.WriteLine("GetValueAsync: " + uri);
        //    webclient.DownloadStringCompleted += cm;
        //    webclient.DownloadStringAsync(uri);
        //}

        /// <summary>
        ///  Send a InvokeMethod request to the sever
        /// </summary>
        public async Task<string> InvokeMethodTaskAsync(string location, string method) {
            // <object>?d?<targetelementid>?[64]<method>
            // TODO: base64 encode method
            try {
                HttpClient webclient =
                    new HttpClient(new HttpClientHandler() {Credentials = new NetworkCredential(Username, Password)});
                var uri = new Uri(GetUrlFromSysUri(location) + "?d??" + method);
                Debug.WriteLine("InvokeMethodTaskAsync: " + uri);
                return await webclient.GetStringAsync(uri);
            }
            catch (Exception ex) {
                throw ex;
            }
        }

        /// <summary>
        ///   Utility function to convert a sys URI (sys://Home/...) to an 
        ///   HTTP URL (http://server/sys/Home/...)
        /// </summary>
        public string GetUrlFromSysUri(string SysUrl) {
            if (SysUrl.StartsWith("sys://")) SysUrl = SysUrl.Remove(0, 6);
            Uri uriServer = SSL
                                ? new UriBuilder("https", Host, Port, "sys/").Uri
                                : new UriBuilder("http", Host, Port, "sys/").Uri;
            return uriServer.AbsoluteUri + SysUrl;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #region IPremiseNotify Members
        // Default implementation of method to set an object property's value
        // Assumes same thread.
        public void DispatchSetMember(PremiseObject obj, string propertyName, string value) {
            obj.SetMember(propertyName, value, false);
        }

        // Default OnPropertyChanged method assumes same thread.
        [NotifyPropertyChangedInvocator]
        public void OnPropertyChanged(PremiseServer thisServer, PropertyChangedEventHandler handler, [CallerMemberName] string propertyName = null) {
            if (handler != null) {
                handler(thisServer, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        #region IDisposable Members
        public void Dispose() {
            if (_subscriptionClient != null)
                _subscriptionClient.Dispose();
            _subscriptionClient = null;
        }
        #endregion
    }
}