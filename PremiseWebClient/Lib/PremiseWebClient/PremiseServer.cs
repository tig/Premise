// Copyright 2014 Kindel Systems
//   

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PremiseWebClient {
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
            get { return _notifier ?? (_notifier = this); }
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
            public Task RequestTask;
            public bool Active;
        }

        private readonly Dictionary<int, Subscription> _subscriptions = new Dictionary<int, Subscription>();
        private IPremiseSocket _subscriptionClient;

        public string Host, Username, Password;
        public int Port;
        public bool Ssl;

        private bool _connected;

        /// <summary>
        /// True if the subscription socket has successfully 
        /// recieved a valid response.
        /// False if the connection is closed.
        /// </summary>
        public bool Connected {
            get { return _connected; }

            set {
                if (value == _connected) return;
                _connected = value;
                Notifier.OnPropertyChanged(this, PropertyChanged);
            }
        }

        private bool _error;

        public bool Error {
            get { return _error; }

            set {
                if (value == _error) return;
                _error = value;
                Notifier.OnPropertyChanged(this, PropertyChanged);
            }
        }

        private string _lastStatusCode;
        public string LastStatusCode {
            get { return _lastStatusCode; }

            set {
                if (value == _lastStatusCode) return;
                _lastStatusCode = value;
                Notifier.OnPropertyChanged(this, PropertyChanged);
            }
        }

        private string _lastError;
        public string LastError {
            get { return _lastError; }

            set {
                if (value == _lastError) return;
                _lastError = value;
                Notifier.OnPropertyChanged(this, PropertyChanged);
            }
        }
        private string _lastResponsePhrase;
        public string LastResponsePhrase {
            get { return _lastResponsePhrase; }

            set {
                if (value == _lastResponsePhrase) return;
                _lastResponsePhrase = value;
                Notifier.OnPropertyChanged(this, PropertyChanged);
            }
        }
        private string _lastConnection;
        public string LastConnection {
            get { return _lastConnection; }

            set {
                if (value == _lastConnection) return;
                _lastConnection = value;
                Notifier.OnPropertyChanged(this, PropertyChanged);
            }
        }        public string LastErrorContentType;
        private string _lastErrorContent;
        public string LastErrorContent {
            get { return _lastErrorContent; }

            set {
                if (value == _lastErrorContent) return;
                _lastErrorContent = value;
                Notifier.OnPropertyChanged(this, PropertyChanged);
            }
        }
        private bool _fastMode;

        /// <summary>
        /// Enable FastMode for the subscription socket.
        /// (Does not apply to Get/Set/Invoke actions).
        /// Setting this to False has no effect; close the 
        /// subscription connection and re-open it to disable
        /// FastMode.
        /// </summary>
        public bool FastMode {
            get { return _fastMode; }

            set {
                if (value == _fastMode) return;
                _fastMode = true;
                EnableFastMode();
                Notifier.OnPropertyChanged(this, PropertyChanged);
            }
        }

        private CancellationTokenSource _readCts;

        /// <summary>
        /// StartSubscriptionsAsync must be called if you want subscription change notifications.
        /// This starts the subscription engine. We always create one subscription for
        /// Home.Name to start (but ignore any updates).
        /// </summary>
        public async Task StartSubscriptionsAsync(IPremiseSocket premiseSocket) {
            try {
                // Always stop the previous subscription client first
                StopSubscriptions();

                if (_readCts != null) {
                    // If we've just cancelled the connection and are reconnecting
                    // we need to wait a bit for the socket to be closed.
                    if (_readCts.IsCancellationRequested)
                        await TaskEx.Delay(1000);
                    _readCts.Dispose();
                }
                _readCts = new CancellationTokenSource();

                _subscriptionClient = premiseSocket;
                Debug.WriteLine("Connecting to IPremiseSocket");
                await _subscriptionClient.ConnectAsync(Host, Port, Ssl, Username, Password);
                Debug.WriteLine("Connected to socket");
                Error = false;
                LastError = "";
                LastErrorContent = "";

                // Assign the resulting task to a local variable to get around
                // the compiler warning about not awaiting this.
                // http://stackoverflow.com/questions/18577054/alternative-to-task-run-that-doesnt-throw-warning
                //ThreadPool.QueueUserWorkItem(state => ReadSubscriptionResponses());
                var readTask = Task.Factory.StartNew(ReadSubscriptionResponses, _readCts.Token);

                // We do a GetValue so we know we have a good connection
                if (await SendRequest("sys://Home?f??" + "Name")) {
                    if (FastMode) EnableFastMode();
                    foreach (var subscription in _subscriptions) {
                        subscription.Value.RequestTask = SendSubscriptionRequest(subscription.Value);
                    }
                }
                else {
                    Debug.WriteLine("SendRequest returned false");
                }
            }
            catch (Exception ex) {
                Debug.WriteLine("StartSubscriptionAsync: " + ex);
                Error = true;
                LastError = ex.GetType().ToString();
                LastErrorContent = ex.Message;
                Dispose();
                throw;
            }
        }

        /// <summary>
        /// Stops the subscription engine. On the server, all subscritpions will be
        /// "forgotten". There's no need to actively unsubscribe them.
        /// </summary>
        public void StopSubscriptions() {
            Debug.WriteLine("StopSubscriptions");
            Dispose();
            foreach (var subscription in _subscriptions) {
                subscription.Value.Active = false;
            }
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
                int hashCode = 0;

                // From here on we will get responses on the stream that
                // look like HTTP POST responses.
                while (_subscriptionClient != null) {
                    _readCts.Token.ThrowIfCancellationRequested(); 
                    
                    var line = await _subscriptionClient.ReadLineAsync();

                    _readCts.Token.ThrowIfCancellationRequested();
                    
                    if (line == null) {
                        Debug.WriteLine("ReadLineAsync returned null. This indicates the server has closed the socket?");
                        break;
                    }
                    Debug.WriteLine(line);

                    if (line.StartsWith("HTTP/1.1 ")) {
                        string statusCode = line.Substring("HTTP/1.1 ".Length, 3);
                        LastStatusCode = statusCode;
                        LastError = line.Substring("HTTP/1.1 ".Length + 4);
                        if (!statusCode.StartsWith("2")) {
                            Debug.WriteLine("Error: " + line);
                            await ParseErrorResponse();
                            Error = true;
                            Dispose();
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
                    int contentLength;
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
                            foreach (var subscription in _subscriptions) {
                                subscription.Value.Active = false;
                            }
                            break;
                        case "resumeConnection":
                            foreach (var subscription in _subscriptions) {
                                Task t = SendSubscriptionRequest(subscription.Value);
                            }
                            break;
                        case "fastMode":
                            FastMode = true;
                            break;
                        default:
                            // We got content!
                            // Find the property this response belongs to and update it
                            Subscription sub;
                            if (_subscriptions.TryGetValue(hashCode, out sub))
                                Notifier.DispatchSetMember(sub.Object, sub.PropertyName, line);
                            break;
                    }
                }
            }
            catch (Exception ex) {
                if (!_readCts.IsCancellationRequested) {
                    Debug.WriteLine("ReadSubscriptionResponse: " + ex.Message);
                    LastError = ex.GetType().ToString();
                    LastErrorContent = ex.Message;
                    Error = true;
                }
                Dispose();
            }
            Debug.WriteLine("Subscription socket closed.");
            Dispose();
        }

        private async Task ParseErrorResponse() {
            try {
                int contentLength = 0;
                while (_subscriptionClient != null) {
                    _readCts.Token.ThrowIfCancellationRequested(); 
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
                throw;
            }
        }

        /// <summary>
        /// Send a subscription request to the server.
        /// </summary>
        /// <param name="po">The object with the property to subscribe.</param>
        /// <param name="propertyName">Name of the property to subscribe.</param>
        public async Task Subscribe(PremiseObject po, string propertyName) {
            try {
                _readCts.Token.ThrowIfCancellationRequested();
                // Ignore multiple subscripitons
                if (_subscriptions.Any(subscription => subscription.Value.PropertyName == propertyName &&
                                                       subscription.Value.Object.Location == po.Location)) {
                    return;
                }

                var sub = new Subscription {Object = po, PropertyName = propertyName};
                _subscriptions.Add(sub.GetHashCode(), sub);
                await SendSubscriptionRequest(sub);

            }
            catch (Exception ex) {
                Debug.WriteLine("Subscribe: " + ex);
                Error = true;
                LastError = ex.GetType().ToString();
                LastErrorContent = ex.Message;
                Dispose();
                throw;
            }
        }

        private async Task SendSubscriptionRequest(Subscription sub) {
            try {
                _readCts.Token.ThrowIfCancellationRequested();
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
                throw;
            }
        }

        /// <summary>
        /// The WebClient server expects HTTP requests like this
        ///    POST /sys/command HTTP/1.1
        ///    User-Agent: some user agent
        ///    Host: hostname:port
        ///    Connection: Keep-Alive
        /// 
        /// 'command' is of the form location?restOfCommand where
        /// 'location' is the path to the object (e.g. Home/Downstairs)
        /// 'restOfCommand' is the command and options (e.g. 'a?...')
        /// 
        /// If the URL does not start with '/sys/' Premise assumes
        /// the client is requesting a static file from disk.
        /// 
        /// </summary>
        private async Task<Boolean> SendRequest(string command, string content = "") {
            try {
                _readCts.Token.ThrowIfCancellationRequested(); 
                if (_subscriptionClient == null) return false;

                var uri = new Uri(GetUrlFromSysUri(command));
                string requestString = "POST " + Uri.EscapeUriString(uri.LocalPath + uri.Query) + " HTTP/1.1\r\n";
                requestString += "User-Agent: PremiseServer .NET Client\r\n";
                requestString += "Host: " + Host + ":" + Port + "\r\n";
                requestString += "Connection: Keep-Alive\r\n";
                requestString += "Authorization: Basic ";
                requestString += Convert.ToBase64String(Encoding.UTF8.GetBytes(Username + ":" + Password)) + "\r\n";
                //requestString += "SYSConnector: true\r\n";
                requestString += "\r\n";
                Debug.WriteLine("SendSubscriptionRequest: " + requestString);
                // Send the request.
                Debug.Assert(_subscriptionClient != null);
                bool b = false;
                try {
                    if (_subscriptionClient != null)
                        b = await _subscriptionClient.WriteStringAsync(requestString);
                }
                catch (Exception ex) {
                    Debug.WriteLine(ex.ToString());
                }
                return b;
            }
            catch (Exception ex) {
                throw;
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
                _readCts.Token.ThrowIfCancellationRequested();

                if (_subscriptionClient == null) return;

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
                throw;
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
                _readCts.Token.ThrowIfCancellationRequested();

                if (_subscriptionClient == null) return;
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
                Debug.WriteLine("Subscribe: " + ex);
                Error = true;
                LastError = ex.GetType().ToString();
                LastErrorContent = ex.Message;
                Dispose();
                throw;
            }
        }

        //============ HttpClient based methods ==================

        /// <summary>
        ///  Send a SetValue request to the sever
        ///   (e.g. url = http://localhost/sys/Home/Kitchen/Overheadlight?e?PowerState, message contains
        ///   "0" or "1"), no response message-body
        /// </summary>
        public void SetValue(string location, string property, string value) {
            var webclient =
                new HttpClient(new HttpClientHandler {Credentials = new NetworkCredential(Username, Password)});
            var uri = new Uri(GetUrlFromSysUri(location) + "?e?" + property);
            Debug.WriteLine("SetValue: " + uri + ": " + value);

            // Premise's implementation of HTTP POST does not return an HTTP response.
            // HttpClient will wait for a response and if it doesn't get one, crash the app
            // Work around this by cancelling the request after sending it.
            webclient.PostAsync(uri, new StringContent(value));
            TaskEx.Delay(100).ContinueWith(task => webclient.CancelPendingRequests());
        }

        /// <summary>
        ///  Send a SetValue request to the sever
        ///   (e.g. url = http://localhost/sys/Home/Kitchen/Overheadlight?e?PowerState, message contains
        ///   "0" or "1"), no response message-body
        /// </summary>
        public async void SetValueAsync(string location, string property, string value) {
            try {
                var webclient =
                    new HttpClient(new HttpClientHandler {Credentials = new NetworkCredential(Username, Password)});
                var uri = new Uri(GetUrlFromSysUri(location) + "?e?" + property);
                Debug.WriteLine("SetValue: " + uri + ": " + value);

                // Premise's implementation of HTTP POST does not return an HTTP response.
                // HttpClient will wait for a response and if it doesn't get one, crash the app
                // Work around this by cancelling the request after sending it.
                await webclient.PostAsync(uri, new StringContent(value)); // no await; return immediately. 
                await TaskEx.Delay(100);
                webclient.CancelPendingRequests();
            } catch (HttpRequestException httpRequestException) {
                return;
            } catch (Exception) {
                throw;
            }
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
                var webclient =
                    new HttpClient(new HttpClientHandler {Credentials = new NetworkCredential(Username, Password)});
                var uri = new Uri(GetUrlFromSysUri(location) + "!" + property);
                Debug.WriteLine("GetValueTaskAsync: " + uri);
                return await webclient.GetStringAsync(uri);
            }
            catch (HttpRequestException httpRequestException) {
                return "";
            }
            catch (Exception) {
                throw;
            }
        }

        /// <summary>
        ///  Send a InvokeMethod request to the sever
        /// </summary>
        public async Task<string> InvokeMethodTaskAsync(string location, string method) {
            // <object>?d?<targetelementid>?[64]<method>
            // TODO: base64 encode method
            try {
                var webclient =
                    new HttpClient(new HttpClientHandler {Credentials = new NetworkCredential(Username, Password)});
                var uri = new Uri(GetUrlFromSysUri(location) + "?d??" + method);
                Debug.WriteLine("InvokeMethodTaskAsync: " + uri);
                return await webclient.GetStringAsync(uri);
            }
            catch (Exception ex) {
                throw;
            }
        }

        /// <summary>
        ///   Utility function to convert a sys URI (sys://Home/...) to an 
        ///   HTTP URL (http://server/sys/Home/...)
        /// </summary>
        public string GetUrlFromSysUri(string sysUrl) {
            if (sysUrl.StartsWith("sys://")) sysUrl = sysUrl.Remove(0, 6);
            Uri uriServer = Ssl
                                ? new UriBuilder("https", Host, Port, "sys/").Uri
                                : new UriBuilder("http", Host, Port, "sys/").Uri;
            return uriServer.AbsoluteUri + sysUrl;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #region IPremiseNotify Members
        // Default implementation of method to set an object property's value
        // Assumes same thread.
        public void DispatchSetMember(PremiseObject obj, string propertyName, string value) {
            obj.SetMember(propertyName, value, false);
        }

        // Default OnPropertyChanged method assumes same thread.
        public void OnPropertyChanged(PremiseServer thisServer, PropertyChangedEventHandler handler, [CallerMemberName] string propertyName = null) {
            if (handler != null) {
                handler(thisServer, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        #region IDisposable Members
        public void Dispose() {
            Debug.WriteLine("Disposing PremiseServer");
            if (_readCts != null)
                _readCts.Cancel();
            if (_subscriptionClient != null)
                _subscriptionClient.Dispose();
            Connected = false;
        }
        #endregion
    }
}