// Copyright 2013 Kindel Systems
//   
//   This is the WinRT Version of PremiseServerBase

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Threading;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Networking;
using Windows.Networking.Sockets;
using PremiseLib.Annotations;
using Windows.Storage.Streams;
using Windows.UI.Core;

namespace PremiseLib
{
    /// <summary>
    /// PremiseServer represents a Premise Server. It is a singleton class
    /// and thus only supports connection to a single server.
    /// 
    /// PremiseServer implements support for the Premise WebClient protocol
    /// spoken by the SYSConnector ActiveX control.
    /// </summary>
    public sealed class PremiseServerBase : IDisposable, INotifyPropertyChanged {

        /// <summary>
        /// True if the subscription socket has successfully 
        /// recieved a valid response.
        /// False if the connection is closed.
        /// </summary>
        public bool Connected {
            get {
                return _connected;
            }

            set {
                if (value == _connected) return;
                _connected = value;
                OnPropertyChanged();
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
                if (CoreApplication.MainView.CoreWindow.Dispatcher.HasThreadAccess)
                    OnPropertyChanged();
                else
                    CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal, () => OnPropertyChanged());
            }
        }

        /// <summary>
        /// StartSubscriptionsAsync must be called if you want subscription change notifications.
        /// This starts the subscription engine. We always create one subscription for
        /// Home DisplayName to start (but ignore any updates).
        /// </summary>
        public async Task StartSubscriptionsAsync(){
            try {
                _subscriptionClient.Control.KeepAlive = true;
                await _subscriptionClient.ConnectAsync(new HostName(Host), Port.ToString(), SocketProtectionLevel.PlainSocket);
                _reader = new DataReader(_subscriptionClient.InputStream);
                _reader.InputStreamOptions = InputStreamOptions.Partial;
                _writer = new DataWriter(_subscriptionClient.OutputStream);
                _socketOpen = true;
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
            _subscriptionClient.Dispose();
        }

        private DataReader _reader = null;
        private DataWriter _writer = null;
        private bool _socketOpen = false;
        private byte[] _subscriptionBuffer = null;
        private StringBuilder _currentLine = new StringBuilder();
        private int _currentLocation = 0;
        private const int BUFFER_SIZE = 1024;

        private async Task<string> ReadLine() {
            try {
                while (_socketOpen){
                    if (_subscriptionBuffer == null || _currentLocation == _subscriptionBuffer.Length) {
                        var bytesAvailable = await _reader.LoadAsync(BUFFER_SIZE);
                        _subscriptionBuffer = new byte[bytesAvailable];
                        _reader.ReadBytes(_subscriptionBuffer);
                        _currentLocation = 0;
                    }

                    Debug.Assert(_subscriptionBuffer != null);
                    if (_subscriptionBuffer == null) return null;

                    while (_currentLocation < _subscriptionBuffer.Length) {
                        char cur = (char) _subscriptionBuffer[_currentLocation++];
                        // Look for new line \r\n
                        if (cur == '\r') continue;
                        if (cur == '\n') {
                            string line = _currentLine.ToString();
                            _currentLine.Clear();
                            return line;
                        }
                        _currentLine.Append(cur);
                    }
                } 
            }
            catch (Exception ex) {
                Debug.WriteLine("ReadLine: " + ex);
            }

            return null;
        }

        private async Task<string> ReadBlock(int n) {
            try {
                while (_socketOpen) {
                    if (_subscriptionBuffer == null || _currentLocation == _subscriptionBuffer.Length) {
                        var bytesAvailable = await _reader.LoadAsync(BUFFER_SIZE);
                        _subscriptionBuffer = new byte[bytesAvailable];
                        _reader.ReadBytes(_subscriptionBuffer);
                        _currentLocation = 0;
                    }

                    Debug.Assert(_subscriptionBuffer != null);
                    if (_subscriptionBuffer == null) return null;

                    while (_currentLine.Length == n || _currentLocation < _subscriptionBuffer.Length) {
                        if (_currentLine.Length == n) {
                            string line = _currentLine.ToString();
                            _currentLine.Clear();
                            return line;
                        }
                        char cur = (char)_subscriptionBuffer[_currentLocation++];
                        _currentLine.Append((char)cur);
                    }
                } while (_socketOpen);
            }
            catch (Exception ex) {
                Debug.WriteLine("ReadLine: " + ex);
            }           return null;
        }

        /// <summary>
        /// Read from the TcpClient socket parsing responses. 
        /// </summary>
        private async void ReadSubscriptionResponses()
        {
            try {
                // Process the response.
                int contentLength;
                int hashCode = 0;

                // From here on we will get responses on the stream that
                // look like HTTP POST responses.
                while (_socketOpen) {
                    var line = await ReadLine();
                    Debug.WriteLine(line);

                    if (line.StartsWith("HTTP/1.1 404 ")) {
                        string error = line.Substring("HTTP/1.1 404 ".Length);
                        Debug.WriteLine("Error: " + error);
                        LastStatusCode = "404";
                        await ParseErrorResponse();
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

                    if (!_socketOpen) continue;

                    // Read the blank line that always follows Content-Length:
                    line = await ReadLine();
                    Debug.WriteLine(line);

                    if (!_socketOpen) continue;

                    // Read content
                    line = await ReadBlock(contentLength);
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
                            sub.Object.SetMember(sub.PropertyName, line, true);
                        break;
                    }
                }
            }
            catch (Exception ex) {
                Debug.WriteLine("ReadSubscriptionResponse: " + ex.ToString());
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

        private async Task ParseErrorResponse() {
            var line = await ReadLine();
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

            if (_socketOpen) return;
            
            // Read the blank line that always follows Content-Length:
            line = await ReadLine();
            Debug.WriteLine(line);

            if (_socketOpen) return;

            // Read content
            line = await ReadBlock(contentLength);
            LastErrorContent = line;
            Debug.WriteLine(line);
        }

        /// <summary>
        /// Send a subscription request to the server.
        /// </summary>
        /// <param name="po">The object with the property to subscribe.</param>
        /// <param name="propertyName">Name of the property to subscribe.</param>
        public async Task Subscribe(PremiseObject po, string propertyName) {
            Debug.Assert(_socketOpen);

            // Ignore multiple subscripitons
            foreach (var subscription in _subscriptions) {
                if (subscription.Value.PropertyName == propertyName &&
                    subscription.Value.Object.Location == po.Location) 
                    return;
            }

            Subscription sub = new Subscription { Object = po, PropertyName = propertyName };
            _subscriptions.Add(sub.GetHashCode(), sub);
            await SendSubscriptionRequest(sub);
        }

        private async Task SendSubscriptionRequest(Subscription sub) {
            if (!String.IsNullOrEmpty(sub.Object.Location))
            {
                // We send as <object>?a?<hashcode>??<property>?<hashcode>?
                //
                // It may not be necessary to set the <subid> (2nd to last param)
                // but we do anyway.
                var command = String.Format("{0}?a?{1}??{2}?{3}?", 
                    sub.Object.Location, sub.GetHashCode(), sub.PropertyName, sub.GetHashCode());

                if (FastMode)
                    await SendRequestFastMode(command, "");
                else {
                    await SendRequest(command, "");
                }
                sub.Active = true;
            }
        }

        private async Task<Boolean> SendRequest(string command, string content = "") {
            var uri = new Uri(GetUrlFromSysUri(command));
            Debug.WriteLine("SendSubscriptionRequest: " + uri.PathAndQuery);

            string requestString = "POST " + uri.PathAndQuery + " HTTP/1.1\r\n";
            requestString += "User-Agent: PremiseServer .NET Client\r\n";
            requestString += "Host: " + Host + ":" + Port + "\r\n";
            requestString += "Connection: Keep-Alive\r\n";
            requestString += "SYSConnector: true\r\n\r\n";
            // Send the request.
            Debug.Assert(_socketOpen);
            bool b = false;
            try {
                _writer.WriteString(requestString);
                await _writer.StoreAsync();
                b = await _writer.FlushAsync();
            }
            catch (Exception ex) {
                Debug.WriteLine(ex.ToString());
            }
            return b;
        }

        private async Task SendRequestFastMode(string command, string content = "") {
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
            Debug.Assert(_socketOpen);
            _writer.WriteString(packet);
            await _writer.FlushAsync();
        }

        private void EnableFastMode() {
            // /sys/{8D692EC9-EB74-4155-9D83-315872AC9800}?e?FastMode
            if (!_socketOpen) return;
            Task t = SendRequest("{8D692EC9-EB74-4155-9D83-315872AC9800}?e?FastMode", "True");
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
                    string command = po.Location + "?c?" + subscription.GetHashCode();
                    Debug.WriteLine("Unsubscribe: " + command);
                    Task t = SendRequest(command);
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
            HttpClient webclient = new HttpClient(new HttpClientHandler() { Credentials = new NetworkCredential(Username, Password) }); 
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
        public async Task<string> GetValueTaskAsync(string location, string property)
        {
            HttpClient webclient = new HttpClient(new HttpClientHandler() { Credentials = new NetworkCredential(Username, Password) });
            var uri = new Uri(GetUrlFromSysUri(location) + "!" + property);
            Debug.WriteLine("GetValueTaskAsync: " + uri);
            return await webclient.GetStringAsync(uri);
        }

        ///// <summary>
        /////  Send a GetValue request to the sever
        ///// </summary>
        ///// <param name="location"></param>
        ///// <param name="property"></param>
        ///// <param name="cm"></param>
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
        public async Task<string> InvokeMethodTaskAsync(string location, string method)
        {
            // <object>?d?<targetelementid>?[64]<method>
            // TODO: base64 encode method
            HttpClient webclient = new HttpClient(new HttpClientHandler() { Credentials = new NetworkCredential(Username, Password) });
            var uri = new Uri(GetUrlFromSysUri(location) + "?d??" + method);
            Debug.WriteLine("InvokeMethodTaskAsync: " + uri);
            return await webclient.GetStringAsync(uri);
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
            _socketOpen = false;
            _subscriptionClient.Dispose();
            _subscriptionClient = null;
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) {
                if (CoreApplication.MainView.CoreWindow.Dispatcher.HasThreadAccess)
                    handler(this, new PropertyChangedEventArgs(propertyName));
                else
                    CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal, () => handler(this, new PropertyChangedEventArgs(propertyName)));
            }
        }
    }
}