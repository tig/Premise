using System;
using System.Threading.Tasks;

namespace PremiseWebClient {
    /// <summary>
    /// Each .NET client platform has it's own socket API. Ideally they'd all support
    /// one, like TcpClient, but they don't. So we define our own socket interface
    /// based on just what Premise requires for the subscripiton protocol.
    /// </summary>
    public interface IPremiseSocket: IDisposable {
        Task ConnectAsync(string hostName, int port, bool ssl, string username, string password);
        Task<string> ReadLineAsync();
        Task<string> ReadBlockAsync(int len);
        Task<bool> WriteStringAsync(string str);
    }
}
