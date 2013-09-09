using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PremiseLib;

namespace PremiseServerTests
{
    [TestClass]
    public class PremiseServerTests
    {
        [TestMethod]
        public void GetPremiseServerInstance() {
            var instance = PremiseServer.Instance;
            Assert.IsInstanceOfType(instance, typeof(PremiseServer));
        }

        [TestMethod]
        public async Task StartSubscriptions() {
            var instance = PremiseServer.Instance;
            instance.Host = "home";
            instance.Port = 86;
            await instance.StartSubscriptionsAsync(new PremiseTcpClientSocket());
        }
    }
}
