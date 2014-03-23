// Copyright 2013 Charlie Kindel
//   
//   

#region using directives

using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

#endregion

namespace PremiseWebClient {
    public sealed class StreamSocketPremiseSocket : IPremiseSocket {
        private const int BufferSize = 1024;
        private readonly StringBuilder _currentLine = new StringBuilder();
        private StreamSocket _client;
        private int _currentLocation;
        private DataReader _reader;

        private byte[] _subscriptionBuffer;
        private DataWriter _writer;

        public async Task ConnectAsync(string hostName, int port, bool ssl, string username, string password) {
            try {
                _client = new StreamSocket();
                _client.Control.KeepAlive = true;
                Debug.WriteLine("Calling StreamSocket.ConnectAsync");
                IAsyncAction action = _client.ConnectAsync(new HostName(hostName), port.ToString(),
                    SocketProtectionLevel.PlainSocket);
                action.AsTask().Wait();
                Debug.WriteLine("StreamSocket.ConnectAsync Done");
                _reader = new DataReader(_client.InputStream) {InputStreamOptions = InputStreamOptions.Partial};
                _writer = new DataWriter(_client.OutputStream);
            }
            catch (Exception ex) {
                var code = SocketError.GetStatus(ex.InnerException.HResult);
                Debug.WriteLine("ConnectAsync: ex.InnerException.HResult = " + code);
                throw ex.InnerException;
            }
        }

        public async Task<string> ReadLineAsync() {
            try {
                while (_client != null) {
                    if (_subscriptionBuffer == null || _currentLocation == _subscriptionBuffer.Length) {
                        var bytesAvailable = await _reader.LoadAsync(BufferSize);
                        _subscriptionBuffer = new byte[bytesAvailable];
                        _reader.ReadBytes(_subscriptionBuffer);
                        _currentLocation = 0;
                    }

                    Debug.Assert(_subscriptionBuffer != null);
                    if (_subscriptionBuffer == null) return null;

                    while (_currentLocation < _subscriptionBuffer.Length) {
                        var cur = (char) _subscriptionBuffer[_currentLocation++];
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
                // 3E3 means thread was aborted. Means we shut down.
                if (ex.HResult != 0x800703e3) {
                    Debug.WriteLine("ReadLineAsync: " + ex.Message);
                    throw;
                }
            }
            return null;
        }

        public async Task<string> ReadBlockAsync(int len) {
            try {
                while (_client != null) {
                    if (_subscriptionBuffer == null || _currentLocation == _subscriptionBuffer.Length) {
                        var bytesAvailable = await _reader.LoadAsync(BufferSize);
                        _subscriptionBuffer = new byte[bytesAvailable];
                        _reader.ReadBytes(_subscriptionBuffer);
                        _currentLocation = 0;
                    }

                    Debug.Assert(_subscriptionBuffer != null);
                    if (_subscriptionBuffer == null) return null;

                    while (_currentLine.Length == len || _currentLocation < _subscriptionBuffer.Length) {
                        if (_currentLine.Length == len) {
                            string line = _currentLine.ToString();
                            _currentLine.Clear();
                            return line;
                        }
                        var cur = (char) _subscriptionBuffer[_currentLocation++];
                        _currentLine.Append(cur);
                    }
                }
            }
            catch (Exception ex) {
                //// 3E3 means thread was aborted. Means we shut down.
                if (ex.HResult == 0x800703E3) return null;
                Debug.WriteLine("ReadBlockAsync: " + ex.Message);
                throw;
            }
            return null;
        }

        public async Task<bool> WriteStringAsync(string str) {
            try {
                Debug.Assert(_writer != null);

                if (_writer != null)
                    _writer.WriteString(str);

                if (_writer != null)
                    await _writer.StoreAsync();
                if (_writer != null)
                    await _writer.FlushAsync();
            }
            catch (Exception ex) {
                // 3E3 means thread was aborted. Means we shut down.
                if (ex.HResult == 0x800703E3) return true;
                Debug.WriteLine("WriteStringAsync: " + ex.Message);
                //throw ex;
                return false;
            }
            return true;
        }

        public void Dispose() {
            Debug.WriteLine("StreamSocketPremiseSocket.Dispose");
            if (_writer != null)
                _writer.Dispose();

            if (_reader != null)
                _reader.Dispose();

            if (_client != null)
                _client.Dispose();
        }
    }
}