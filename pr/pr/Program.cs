using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace pr
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Premise");
            var pr = new Program();
            pr.Test();

            Console.ReadLine();
        }

        public async void Test() {
            PremiseServer server = PremiseServer.Instance;
            server.Host = "";
            server.Port = 80;
            server.Username = "";
            server.Password = "";

            Task<string> responseTask = server.Connect();

            Console.WriteLine("Waiting for connection...");
            string response = await responseTask;
            Console.WriteLine("Response: {0}", response);

            Console.WriteLine("Upstairs Occupancy: {0}", await server.GetPropertyAsync("sys://Home/Upstairs", "Occupancy"));
            Console.WriteLine("Downstairs Occupancy: {0}", await server.GetPropertyAsync("sys://Home/Downstairs", "Occupancy"));

            dynamic o = new PremiseObject();
            
            int n = await o.Init("sys://Home/Downstairs/Office/Desk", "DisplayName", "PowerState", "Brightness");
            Console.WriteLine("Number of properties: {0}", n);

            Console.WriteLine("Office Desk DisplayName: {0}", o.DisplayName);
            Console.WriteLine("Office Desk PowerState: {0}", o.PowerState);
            Console.WriteLine("Office Desk Brightness: {0}", o.Brightness);

            o.DisplayName = "Charlie_s New Desk";
            Console.WriteLine("Office Desk DisplayName: {0}", o.DisplayName);

        }
    }
}
