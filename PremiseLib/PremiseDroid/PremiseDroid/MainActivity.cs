using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using PremiseLib;
using System.Net;
using System.IO;

namespace PremiseDroid
{
	[Activity (Label = "PremiseDroid", MainLauncher = true)]
	public class MainActivity : Activity
	{
		private readonly PremiseServer _server = PremiseServer.Instance;
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Main);

			// Get our button from the layout resource,
			// and attach an event to it
			Button buttonStart = FindViewById<Button> (Resource.Id.buttonStart);
			Button buttonTrigger = FindViewById<Button> (Resource.Id.buttonTrigger);
			TextView textConnected = FindViewById<TextView> (Resource.Id.textConnected);
			TextView textStatus = FindViewById<TextView> (Resource.Id.textKeypadStatus1);
			TextView textDescription = FindViewById<TextView> (Resource.Id.textKeyPad1);

			_server.Notifier = new MonoIPremiseNotify ();

			_server.Host = "192.168.0.2";
			_server.Port = 86;
			_server.Username = "";
			_server.Password = "";			
			_server.PropertyChanged += (sender, args) => {
				if (args.PropertyName == "Error" && _server.Error) {
					Console.WriteLine ("Error communicating with {0}", _server.Host);
					Console.WriteLine ("HTTP status code: " + _server.LastStatusCode);
					Console.WriteLine ("HTTP error: " + _server.LastError);
				}
				if (args.PropertyName == "Connected") {
					Console.WriteLine ("{0} {1}", _server.Connected ? "Connected to" : "Disconnected from", _server.Host);
					textConnected.Text = "Connected";
				}
				if (args.PropertyName == "FastMode")
					Console.WriteLine ("FastMode is: {0}", ((PremiseServer)sender).FastMode);
			};

			buttonStart.Click += async (object s, EventArgs ea) => {
				try {
					await _server.StartSubscriptionsAsync(new PremiseTcpClientSocket());
					_server.FastMode = true;

					PremiseObject ob = new PremiseObject("sys://Home/Downstairs/Office/Office At Entry Door/Button_Workshop");
					ob.PropertyChanged += (sender, args) => {
						var propName = PremiseProperty.NameFromItem(args.PropertyName);
						var val = ((PremiseObject)sender)[propName];
						Console.WriteLine("{0}: {1} = {2}", ((PremiseObject)sender).Location, propName, val);

						if (propName == "Status") textStatus.Text = val.ToString();
						if (propName == "Description") textDescription.Text = val.ToString();
					};

					await ob.AddPropertyAsync("Description");
					await ob.AddPropertyAsync("Trigger", PremiseProperty.PremiseType.TypeBoolean, false);
					await ob.AddPropertyAsync("Status", PremiseProperty.PremiseType.TypeBoolean, true);
					buttonTrigger.Click += (sender, e) => { 
						((dynamic)ob["TriggerCommand"]).Execute(null); 
					};
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
				catch (Exception ex) {
					Console.WriteLine("Exception: {0}", ex.Message);
				}
			};
		}
	}
}


