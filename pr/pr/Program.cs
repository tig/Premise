using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using ManyConsole;

namespace pr
{
    class Program
    {
        // Define a class to receive parsed values
        class Options {
            [Option('h', "host", Required = false, DefaultValue = "home", HelpText = "Premise server hostname or IP address.")]
            public string Host { get; set; }

            [Option('p', "port", Required = true, DefaultValue = 86, HelpText = "Port Premise server is listening on.")]
            public int Port { get; set; }

            [Option('s', "ssl", Required = false, DefaultValue = false, HelpText = "Use SSL?")]
            public bool Ssl { get; set; }

            [Option('u', "username", Required = true, HelpText = "Username.")]
            public string Username { get; set; }

            [Option('w', "password", Required = true, HelpText = "Password.")]
            public string Password { get; set; }

            [ParserState]
            public IParserState LastParserState { get; set; }

            [HelpOption]
            public string GetUsage()
            {
                return HelpText.AutoBuild(this,
                  (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
            }
        }

        static void Main(string[] args){
            Console.WriteLine("Premise");

            var options = new Options();
            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                // Values are available here
                var pr = new Program();
                pr.Test(options.Host, options.Port, options.Ssl, options.Username, options.Password);
            }

            Console.ReadLine();
        }

        public static IEnumerable<ConsoleCommand> GetCommands() {
            return ConsoleCommandDispatcher.FindCommandsInSameAssemblyAs(typeof(Program));
        }

        public async void Test(string host, int port, bool ssl, string username, string password) {
            PremiseServer server = PremiseServer.Instance;
            server.Host = host;
            server.Port = port;
            server.Username = username;
            server.Password = password;

            Console.WriteLine("Connecting to {0}:{1} (SSL is {2})", host, port, ssl);
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
