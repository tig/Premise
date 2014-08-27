// Copyright 2013 Charlie Kindel
//   
//   A command line app for testing and playing with the
//   Premise WebClient .NET Client Library

#region using directives

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using ManyConsole;
using Microsoft.CSharp.RuntimeBinder;
using PremiseWebClient;

#endregion

namespace pr {
    internal class Program {
        private readonly PremiseServer _server = PremiseServer.Instance;
        private static void Main(string[] args) {
            Console.SetOut(new PrDebugWriter());
            var options = new Options();
            if (Parser.Default.ParseArguments(args, options)) {
                if (String.IsNullOrEmpty(options.Username)) {
                    Console.Write("Username: ");
                    options.Username = Console.ReadLine();
                }

                if (String.IsNullOrEmpty(options.Password)) {
                    Console.Write("Password: ");
                    ConsoleKeyInfo key;
                    do {
                        key = Console.ReadKey(true);

                        // Ignore any key out of range. 
                        if (((int)key.Key) >= 65 && ((int)key.Key <= 90)) {
                            // Append the character to the password.
                            options.Password += key.KeyChar;
                            Console.Write("*");
                        }
                        // Exit if Enter key is pressed.
                    } while (key.Key != ConsoleKey.Enter);
                    Console.WriteLine();
                } 

                Console.WriteLine("Press Q to quit.");
                var pr = new Program();
                pr.Test(options.Host, options.Port, options.Ssl, options.Username, options.Password, options.Fastmode);
                while (Console.ReadKey().Key != ConsoleKey.Q) ;
            }
        }

        private static IEnumerable<ConsoleCommand> GetCommands() {
            return ConsoleCommandDispatcher.FindCommandsInSameAssemblyAs(typeof (Program));
        }

        public async void Test(string host, int port, bool ssl, string username, string password, bool fastmode) {
            Console.WriteLine("Host: {0}, Port: {1}, SSL: {2}, Username: {3}, Password: {4}, Fastmode: {5}",
                host, port, ssl, username, new string ('*', password.Length), fastmode);

            _server.Notifier = new TestNotifier();

            _server.Host = host;
            _server.Port = port;
            _server.Username = username;
            _server.Password = password.ToString();

            _server.PropertyChanged += (sender, args) => {
                if (args.PropertyName == "Error" && _server.Error) {
                    Console.WriteLine("Error communicating with {0}", _server.Host);
                    Console.WriteLine("HTTP status code: " + _server.LastStatusCode);
                    Console.WriteLine("HTTP error: " + _server.LastError);
                }
                if (args.PropertyName == "Connected")
                    Console.WriteLine("{0} {1}", _server.Connected ? "Connected to" : "Disconnected from", _server.Host);
                if (args.PropertyName == "FastMode")
                    Console.WriteLine("FastMode: {0}", ((PremiseServer) sender).FastMode);
            };

            try {
                _server.FastMode = fastmode;
                await _server.StartSubscriptionsAsync(new PremiseTcpClientSocket());

                //_server.SetValueAsync("sys://Home/Admin/Garage Door Power", "State", "False");
                //_server.SetValueAsync("sys://Home/Admin/Garage Door Power", "State", "True");
                // Don't await to prove that it works async.
                //TestSubscriptions();

                //await TestRecurringUpdate();

                //await TestGetXML();

                await TestCommandExecute();

                //await TestInvokeMethod();

                //TestGetLights();

            }
            catch (System.Net.Sockets.SocketException socketException) {
                Console.WriteLine("SocketException: {0}", socketException.Message);
            }
            catch (System.Net.Http.HttpRequestException httpRequestException) {
                Console.WriteLine("HttpRequestException: {0} {1}", httpRequestException.Message, 
                    httpRequestException.InnerException == null ? "" :httpRequestException.InnerException.Message);
            }
            catch (WebException we) {
                Console.WriteLine("WebException: {0}", we.Message);
                var rdr = new StreamReader(we.Response.GetResponseStream());
                while (!rdr.EndOfStream) {
                    Console.WriteLine("  " + rdr.ReadLine());
                }
            }
            catch (RuntimeBinderException be) {
                Console.WriteLine("RuntimeBinderException: {0}", be.Message);
            }
        }
        
        private async Task TestInvokeMethod() {
            string s = await _server.InvokeMethodTaskAsync("sys://Home/Downstairs/Office", "CheckIsOfExplicitType(\"sys://Schema/Home/Location\")");
            Console.WriteLine("Result: {0}", s);


        }

        private async Task TestSubscriptions() {
            PremiseObject ob = new PremiseObject("sys://Home/Downstairs/Office/Undercounter");
            ob.PropertyChanged += (sender, args) => {
                var propName = PremiseProperty.NameFromItem(args.PropertyName);
                if (propName != null) {
                    var val = ((PremiseObject) sender)[propName];
                    Console.WriteLine("TestSubscriptions {0}: {1} = {2}", ((PremiseObject) sender).Location, propName,
                        val);
                }
            };

            ob.AddPropertyAsync("Brightness", PremiseProperty.PremiseType.TypePercent, true);
            ob.AddPropertyAsync("PowerState", PremiseProperty.PremiseType.TypeBoolean, false);

            //Thread.Sleep(1000);
            _server.Subscribe(ob, "PowerState");

            double d = 0;
            ob["Brightness"] = d;
            //await _server.Unsubscribe(ob, "Brightness");
            //Thread.Sleep(1);
            //ob["Brightness"] = d = d + .1;
            //Thread.Sleep(1);
            //ob["Brightness"] = d = d + .1;
            //Thread.Sleep(1);
            //ob["Brightness"] = d = d + .1;
            //Thread.Sleep(1);
            //ob["Brightness"] = d = d + .1;
            //Thread.Sleep(1);
            //ob["Brightness"] = d = d + .1;
            //Thread.Sleep(1000);
            //ob["Brightness"] = d = d + .1;
            //Thread.Sleep(1000);
            //ob["Brightness"] = d = d + .1;

            //Thread.Sleep(2000);
            //await _server.Subscribe(ob, "Brightness");

            //ob["Brightness"] = "33%";
            ////((dynamic) ob).Brightness = ((dynamic) ob).Brightness + .25;
        }

        private async Task TestRecurringUpdate() {
            //sys://Home/Admin/EqupTemp_VoltageSensor
            PremiseObject voltage = await WatchObjectAsync("sys://Home/Downstairs/Office/Equipment Room/Kindel_TemperatureSensor",
                                                          new PremiseProperty("Name",
                                                                              PremiseProperty.PremiseType.TypeText),
                                                          new PremiseProperty("Temperature",
                                                                              PremiseProperty.PremiseType.TypeFloat));
            //voltage.PropertyChanged += (sender, args) => {
            //    var propName = PremiseProperty.NameFromItem(args.PropertyName);
            //    var val = ((PremiseObject)sender)[propName];
            //    Console.WriteLine("TestRecurringUpdate {0}: {1} = {2}", ((PremiseObject)sender).Location, propName, val);
            //};
        }

        private async Task TestGetLights() {
            string s = await _server.InvokeMethodTaskAsync("sys://Home/Admin/WebClientMethods", "GetLights(\"sys://Home/Downstairs/Office\")");
            Console.WriteLine("Result: {0}", s);
        }

        private async Task TestGetXML() {
            //XmlDocument doc = new XmlDocument();
            //doc.LoadXml(((dynamic)office)._xml);
            //XmlNodeList nodes = doc.SelectNodes("//Object");
            //foreach (XmlElement n in nodes) {
            //    Console.WriteLine("{0} {1}", n.Attributes["ID"].Value, n.Attributes["Name"].Value);
            //}

            //// This can take a while with a lot of objects
            //var home = new PremiseObject("sys://Home/Downstairs");
            //home.PropertyChanged += (sender, args) => {
            //    var val = ((PremiseObject)sender).GetMember(args.PropertyName);
            //    Console.WriteLine("{0}: {1} = {2}", ((PremiseObject)sender).Location, args.PropertyName, val);
            //};

            //await home.AddPropertyAsync("_xml", PremiseProperty.PremiseType.TypeText, true);

            ////Load xml
            //XDocument xdoc = XDocument.Parse(((dynamic)home)._xml);

            ////Run query
            //var objs = from obj in xdoc.Descendants("Object")
            //           where
            //               obj.Attribute("Class").Value.Contains("sys://Schema/Device")
            //           select obj;

            ////Loop through results
            //foreach (var obj in objs) {
            //    var properties = new List<PremiseProperty>();
            //    var o = new PremiseObject(obj.Attribute("ID").Value);
            //    o.PropertyChanged += (sender, args) => {
            //        var val = ((PremiseObject)sender).GetMember(args.PropertyName);
            //        Console.WriteLine("{0}: {1} = {2}", ((PremiseObject)sender).Location, args.PropertyName, val);
            //    };

            //    foreach (var att in obj.Attributes()) {
            //        if (att.Name.ToString().Equals("ID")) continue;
            //        if (att.Name.ToString().Equals("Class")) continue;
            //        if (att.Name.ToString().Equals("Flags")) continue;
            //        if (att.Name.ToString().Equals("OccupancyLastTrigger")) continue;
            //        if (att.Name.ToString().Equals("OccupancyTimeTriggered")) continue;
            //        if (att.Name.ToString().Equals("Script")) continue;
            //        if (att.Name.ToString().Equals("BoundObject")) continue;
            //        if (att.Name.ToString().Equals("TargetProperty")) continue;
            //        Task tast = o.AddPropertyAsync(att.Name.ToString(), PremiseProperty.PremiseType.TypeText, true);
            //    }
            //}            
        }

        private async Task TestCommandExecute() {

            var buttons = new KeypadButtons();
            foreach (PremiseObject button in buttons) {
                button.PropertyChanged += (sender, args) => {
                    var propName = PremiseProperty.NameFromItem(args.PropertyName);
                    if (propName == null) return;
                    var val = ((PremiseObject)sender)[propName];
                    Console.WriteLine("TestCommandExecute {0}: {1} = {2}", ((PremiseObject)sender).Location, propName, val);
                };
            }
            buttons[0]["TriggerCommand"].Execute(null);
            Thread.Sleep(5000);
            buttons[0]["TriggerCommand"].Execute(null);

            dynamic GDOPower;
            GDOPower = new PremiseObject("sys://Home/Admin/Garage Door Power");
            await GDOPower.AddPropertyAsync("DisplayName", "GDO Power", PremiseProperty.PremiseType.TypeText, true);
            await GDOPower.AddPropertyAsync("State", PremiseProperty.PremiseType.TypeBoolean, true);

            GDOPower["StateCommand"].Execute(false);
            Thread.Sleep(1000);
            GDOPower["StateCommand"].Execute(true);

        }

        private class KeypadButtons : ObservableCollection<dynamic> {
            public KeypadButtons() : base() {
                Add(new PremiseObject("sys://Home/Downstairs/Office/Office At Entry Door/Button_Workshop"));
                //Add(new PremiseObject("sys://Home/Downstairs/Office/Office At Entry Door/Button_Equipment"));

                foreach (PremiseObject o in this) {
                    o.AddPropertyAsync("Name", PremiseProperty.PremiseType.TypeText);
                    o.AddPropertyAsync("DisplayName", PremiseProperty.PremiseType.TypeText);
                    o.AddPropertyAsync("Description", PremiseProperty.PremiseType.TypeText);
                    o.AddPropertyAsync("Trigger", PremiseProperty.PremiseType.TypeBoolean);
                    o.AddPropertyAsync("Status", PremiseProperty.PremiseType.TypeBoolean, true);
                }
            }
        }


        // helper
        public async Task<PremiseObject> WatchObjectAsync(string location, params PremiseProperty[] properties) {
            var o = new PremiseObject(location);
            o.PropertyChanged += (sender, args) => {
                var propName = PremiseProperty.NameFromItem(args.PropertyName);
                if (propName == null) return;
                var val = ((PremiseObject)sender)[propName];
                Console.WriteLine("{0}: {1} = {2}", ((PremiseObject)sender).Location, propName, val);
            };

            await o.AddPropertiesAsync(true, properties);
            return o;
        }

        private class Options {
            [Option('h', "host", Required = false, DefaultValue = "home",
                HelpText = "Premise server hostname or IP address.")]
            public string Host { get; set; }

            [Option('p', "port", Required = true, DefaultValue = 80, HelpText = "Port Premise server is listening on.")]
            public int Port { get; set; }

            [Option('s', "ssl", Required = false, DefaultValue = false, HelpText = "Use SSL?")]
            public bool Ssl { get; set; }

            [Option('u', "username", Required = false, HelpText = "Username.")]
            public string Username { get; set; }

            [Option('w', "password", Required = false, HelpText = "Password.")]
            public string Password { get; set; }

            [Option('f', "fastmode", Required = false, DefaultValue = false, HelpText = "Fast mode.")]
            public bool Fastmode { get; set; }

            [ParserState]
            public IParserState LastParserState { get; set; }

            [HelpOption]
            public string GetUsage() {
                var help = new HelpText
                {
                    Heading = new HeadingInfo("Premise WebClient Test App", "1.0"),
                    Copyright = new CopyrightInfo("Kindel Systems", 2013),
                    AdditionalNewLineAfterOption = true,
                    AddDashesToOption = true
                };
                help.AddOptions(this);
                return help; 
            }
        }

        public class PrDebugWriter : StringWriter {
            //save static reference to stdOut
            static TextWriter stdOut = Console.Out;

            public override void WriteLine(string value) {
                Debug.WriteLine(value);
                stdOut.WriteLine(value);
                base.WriteLine(value);
            }

            public override void Write(string value) {
                Debug.Write(value);
                stdOut.Write(value);
                base.Write(value);
            }

            public override Encoding Encoding {
                get { return Encoding.Unicode; }
            }
        }
    }
}