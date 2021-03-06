﻿/// WinRT Version

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using PremiseWebClient;
using Windows.Networking;
using Windows.Networking.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using PremiseLib.Annotations;
using Windows.Storage.Streams;

namespace PremiseMetro {
    public class StreamSocketPremiseSocket : IPremiseSocket {
        private StreamSocket _client = new StreamSocket();
        private DataReader _reader;
        private DataWriter _writer;

        private byte[] _subscriptionBuffer = null;
        private StringBuilder _currentLine = new StringBuilder();
        private int _currentLocation = 0;
        private const int BUFFER_SIZE = 1024;

        public async Task ConnectAsync(string hostName, int port, bool ssl, string username, string password) {
            try {
                _client.Control.KeepAlive = true;
                await _client.ConnectAsync(new HostName(hostName), port.ToString(), SocketProtectionLevel.PlainSocket); 
                _reader = new DataReader(_client.InputStream) {InputStreamOptions = InputStreamOptions.Partial};
                _writer = new DataWriter(_client.OutputStream);
            } catch (Exception ex) {
                Debug.WriteLine(ex.ToString());
                throw ex;
            }
        }

        public async Task<string> ReadLineAsync() {
            try {
                while (_client != null) {
                    if (_subscriptionBuffer == null || _currentLocation == _subscriptionBuffer.Length) {
                        var bytesAvailable = await _reader.LoadAsync(BUFFER_SIZE);
                        _subscriptionBuffer = new byte[bytesAvailable];
                        _reader.ReadBytes(_subscriptionBuffer);
                        _currentLocation = 0;
                    }

                    Debug.Assert(_subscriptionBuffer != null);
                    if (_subscriptionBuffer == null) return null;

                    while (_currentLocation < _subscriptionBuffer.Length) {
                        char cur = (char)_subscriptionBuffer[_currentLocation++];
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
            } catch (Exception ex) {
                Debug.WriteLine("ReadLine: " + ex);
                Dispose();
                throw ex;
            }
            return null;
        }

        public async Task<string> ReadBlockAsync(int len) {
            try {
                while (_client != null) {
                    if (_subscriptionBuffer == null || _currentLocation == _subscriptionBuffer.Length) {
                        var bytesAvailable = await _reader.LoadAsync(BUFFER_SIZE);
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
                        char cur = (char)_subscriptionBuffer[_currentLocation++];
                        _currentLine.Append((char)cur);
                    }
                } 
            } catch (Exception ex) {
                Debug.WriteLine("ReadLine: " + ex);
                Dispose();
                throw ex;
            }
            return null;
        }

        public async Task<bool> WriteStringAsync(string str) {
            try {
                _writer.WriteString(str);
                await _writer.StoreAsync();
                await _writer.FlushAsync();
            } catch (Exception e) {
                Dispose();
                throw e;
            }
            return true;
        }

        public void Dispose() {
            _writer.Dispose();
            _writer = null;
            _reader.Dispose();
            _reader = null;
            _client.Dispose();
            _client = null;
        }
    }
}
