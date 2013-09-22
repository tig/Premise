// .NET 4.5 version

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using PremiseWebClient;

namespace pr {
    public class PremiseTcpClientSocket : IPremiseSocket {
        private TcpClient _client = new TcpClient();
        private StreamReader _reader = null;
        private StreamWriter _writer = null;

        public async Task ConnectAsync(string hostName, int port, bool ssl, string username, string password) {
            try {
                await _client.ConnectAsync(hostName, port);
                _reader = new StreamReader(_client.GetStream());
                _writer = new StreamWriter(_client.GetStream());
            } catch (Exception ex) {
                Debug.WriteLine(ex.ToString());
                throw ex;
            }
        }

        public async Task<string> ReadLineAsync() {
            try {
                return await _reader.ReadLineAsync();
            } catch (Exception e) {
                Debug.WriteLine(e.ToString());
                throw e;
            }
        }

        public async Task<string> ReadBlockAsync(int len) {
            try {
                char[] buffer = new char[len];
                await _reader.ReadBlockAsync(buffer, 0, len);
                return new string(buffer);
            } catch (Exception e) {
                throw e;
            }
        }

        public async Task<bool> WriteStringAsync(string str) {
            try {
                await _writer.WriteAsync(str);
                await _writer.FlushAsync();
            } catch (Exception e) {
                throw e;
            }
            return true;
        }

        public void Dispose() {
            _writer.Close();
            _writer = null;
            _reader.Close();
            _reader = null;
            _client.Close();
        }
    }
}
