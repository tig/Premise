// Copyright 2013 Charlie Kindel
//   
//   A command line app for testing and playing with the
//   Premise WebClient .NET Client Library

#region using directives

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using CommandLine;
using CommandLine.Text;
using ManyConsole;
using Microsoft.CSharp.RuntimeBinder;

#endregion

namespace PremiseLib {
    /// <summary>
    /// </summary>
    internal class Program {
        // Define a class to receive parsed values

        private readonly PremiseServer _server = PremiseServer.Instance;

        private static void Main(string[] args) {
            Console.SetOut(new PrDebugWriter());

            Console.WriteLine("Premise");

            var options = new Options();
            if (Parser.Default.ParseArguments(args, options)) {
                if (String.IsNullOrEmpty(options.Username)) {
                    Console.Write("Username: ");
                    options.Username = Console.ReadLine();
                }

                if (String.IsNullOrEmpty(options.Password)) {
                    Console.Write("Password: ");
                    options.Password = Console.ReadLine();
                }

                // Values are available here
                var pr = new Program();
                pr.Test(options.Host, options.Port, options.Ssl, options.Username, options.Password);
                Console.WriteLine("Press Q to quit.");
            }

            while (Console.ReadKey().Key != ConsoleKey.Q) ;
        }

        public static IEnumerable<ConsoleCommand> GetCommands() {
            return ConsoleCommandDispatcher.FindCommandsInSameAssemblyAs(typeof (Program));
        }

        public async void Test(string host, int port, bool ssl, string username, string password) {
            _server.Host = host;
            _server.Port = port;
            _server.Username = username;
            _server.Password = password;

            _server.PropertyChanged += (sender, args) => {
                if (args.PropertyName == "Connected")
                    Console.WriteLine("Server: {0} = {1}", args.PropertyName, ((PremiseServer) sender).Connected);
                if (args.PropertyName == "FastMode")
                    Console.WriteLine("Server: {0} = {1}", args.PropertyName, ((PremiseServer) sender).FastMode);
            };

            try {
                Console.WriteLine("Starting Subscriptions on {0}:{1} (SSL is {2})", host, port, ssl);
                await _server.StartSubscriptionsAsync();
                _server.FastMode = true;

                //PremiseObject ob = new PremiseObject("sys://Home/Downstairs/Office/Undercounter");
                //ob.PropertyChanged += (sender, args) => {
                //    var val = ((PremiseObject)sender).GetMember(args.PropertyName);
                //    Console.WriteLine("{0}: {1} = {2}", ((PremiseObject)sender).Location, args.PropertyName, val);
                //};

                //await ob.AddPropertiesAsync();
                //await ob.AddPropertyAsync("Brightness", PremiseProperty.PremiseType.TypePercent, true);
                ////await ob.AddPropertyAsync("PowerState", PremiseProperty.PremiseType.TypeBoolean, true);

                //((dynamic) ob).Brightness = ((dynamic) ob).Brightness - .15;
                ////await ob.AddPropertyAsync("Flags", PremiseProperty.PremiseType.TypeText, true);
                //ob.AddPropertyAsync("_xml", PremiseProperty.PremiseType.TypeText, true);

                //((dynamic)ob).Brightness = "33%";
                //((dynamic)ob).Brightness = ((dynamic)ob).Brightness + .25;

                //PremiseObject motion = new PremiseObject("sys://Home/Downstairs/Office/Motion Detector");
                //motion.PropertyChanged += (sender, args) =>
                //{
                //    var val = ((PremiseObject)sender).GetMember(args.PropertyName);
                //    Console.WriteLine("{0}: {1} = {2}", ((PremiseObject)sender).Location, args.PropertyName, val);
                //};

                //await motion.AddPropertiesAsync();
                //await motion.AddPropertyAsync("MotionDetected", subscribe: true);
                //await motion.AddPropertyAsync("LastTimeTriggered", PremiseProperty.PremiseType.TypeDateTime, subscribe: true);

                //Console.WriteLine("{0:F}", ((dynamic)motion).LastTimeTriggered);

                // sys://Home/Admin/EqupTemp_VoltageSensor
                //PremiseObject voltage = await WatchObjectAsync("sys://Home/Admin/EqupTemp_VoltageSensor",
                //                                              new PremiseProperty("Name",
                //                                                                  PremiseProperty.PremiseType.TypeText),
                //                                              new PremiseProperty("Voltage",
                //                                                                  PremiseProperty.PremiseType.TypeFloat));
                //voltage.PropertyChanged += (sender, args) =>
                //{
                //    //((dynamic)ob).Brightness = ((dynamic)voltage).Voltage / 2;
                //    ((dynamic) office).Occupancy = !((dynamic) office).Occupancy;
                //};
                //string result = await _server.InvokeMethodTaskAsync("{A2214A6E-1A22-4A67-AEC7-CDB863C316BB}", "GetButtons()");
                //foreach (string s in result.Split(',')) {
                //    await WatchObjectAsync(s,
                //        new PremiseProperty("Status",  PremiseProperty.PremiseType.TypeBoolean),
                //        new PremiseProperty("Trigger", PremiseProperty.PremiseType.TypeBoolean));
                //}


                //XmlDocument doc = new XmlDocument();
                //doc.LoadXml(((dynamic)office)._xml);
                //XmlNodeList nodes = doc.SelectNodes("//Object");
                //foreach (XmlElement n in nodes) {
                //    Console.WriteLine("{0} {1}", n.Attributes["ID"].Value, n.Attributes["Name"].Value);
                //}

                // This can take a while with a lot of objects
                var home = new PremiseObject("sys://Home/Downstairs");
                home.PropertyChanged += (sender, args) => {
                    var val = ((PremiseObject) sender).GetMember(args.PropertyName);
                    Console.WriteLine("{0}: {1} = {2}", ((PremiseObject) sender).Location, args.PropertyName, val);
                };

                await home.AddPropertyAsync("_xml", PremiseProperty.PremiseType.TypeText, true);

                //Load xml
                XDocument xdoc = XDocument.Parse(((dynamic)home)._xml);

                //Run query
                var objs = from obj in xdoc.Descendants("Object")
                           where
                               obj.Attribute("Class").Value.Contains("sys://Schema/Device")
                           select obj;

                //Loop through results
                foreach (var obj in objs) {
                    var properties = new List<PremiseProperty>();
                    var o = new PremiseObject(obj.Attribute("ID").Value);
                    o.PropertyChanged += (sender, args) => {
                        var val = ((PremiseObject)sender).GetMember(args.PropertyName);
                        Console.WriteLine("{0}: {1} = {2}", ((PremiseObject)sender).Location, args.PropertyName, val);
                    };

                    foreach (var att in obj.Attributes()) {
                        if (att.Name.ToString().Equals("ID")) continue;
                        if (att.Name.ToString().Equals("Class")) continue;
                        if (att.Name.ToString().Equals("Flags")) continue;
                        if (att.Name.ToString().Equals("OccupancyLastTrigger")) continue;
                        if (att.Name.ToString().Equals("OccupancyTimeTriggered")) continue;
                        if (att.Name.ToString().Equals("Script")) continue;
                        if (att.Name.ToString().Equals("BoundObject")) continue;
                        if (att.Name.ToString().Equals("TargetProperty")) continue;
                        Task tast = o.AddPropertyAsync(att.Name.ToString(), PremiseProperty.PremiseType.TypeText, true);
                    }
                }
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

        // helper
        public async Task<PremiseObject> WatchObjectAsync(string location, params PremiseProperty[] properties) {
            var o = new PremiseObject(location);
            o.PropertyChanged += (sender, args) => {
                var val = ((PremiseObject) sender).GetMember(args.PropertyName);
                Console.WriteLine("{0}: {1} = {2}", ((PremiseObject) sender).Location, args.PropertyName, val);
            };

            await o.AddPropertiesAsync(true, properties);
            return o;
        }

        private class Options {
            [Option('h', "host", Required = false, DefaultValue = "home",
                HelpText = "Premise server hostname or IP address.")]
            public string Host { get; set; }

            [Option('p', "port", Required = true, DefaultValue = 86, HelpText = "Port Premise server is listening on.")]
            public int Port { get; set; }

            [Option('s', "ssl", Required = false, DefaultValue = false, HelpText = "Use SSL?")]
            public bool Ssl { get; set; }

            [Option('u', "username", Required = false, HelpText = "Username.")]
            public string Username { get; set; }

            [Option('w', "password", Required = false, HelpText = "Password.")]
            public string Password { get; set; }

            [ParserState]
            public IParserState LastParserState { get; set; }

            [HelpOption]
            public string GetUsage() {
                return HelpText.AutoBuild(this,
                                          (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
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