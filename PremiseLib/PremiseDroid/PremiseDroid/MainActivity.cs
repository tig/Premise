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
	[Activity (Label = "Garage Doors", MainLauncher = true)]
	public class MainActivity : Activity
	{
		private readonly PremiseServer _server = PremiseServer.Instance;
		protected override async void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Main);

			// Get our button from the layout resource,
			// and attach an event to it
			TextView textConnected = FindViewById<TextView> (Resource.Id.textConnected);

			TextView textLowerWest = FindViewById<TextView> (Resource.Id.textLowerWest);
			TextView textLowerWestStatus = FindViewById<TextView> (Resource.Id.textLowerWestStatus);
			Button buttonLowerWest = FindViewById<Button> (Resource.Id.buttonLowerWest);

			TextView textLowerEast = FindViewById<TextView> (Resource.Id.textLowerEast);
			TextView textLowerEastStatus = FindViewById<TextView> (Resource.Id.textLowerEastStatus);
			Button buttonLowerEast = FindViewById<Button> (Resource.Id.buttonLowerEast);

			TextView textUpperWest = FindViewById<TextView> (Resource.Id.textUpperWest);
			TextView textUpperWestStatus = FindViewById<TextView> (Resource.Id.textUpperWestStatus);
			Button buttonUpperWest = FindViewById<Button> (Resource.Id.buttonUpperWest);

			TextView textUpperCenter = FindViewById<TextView> (Resource.Id.textUpperCenter);
			TextView textUpperCenterStatus = FindViewById<TextView> (Resource.Id.textUpperCenterStatus);
			Button buttonUpperCenter = FindViewById<Button> (Resource.Id.buttonUpperCenter);

			TextView textUpperEast = FindViewById<TextView> (Resource.Id.textUpperEast);
			TextView textUpperEastStatus = FindViewById<TextView> (Resource.Id.textUpperEastStatus);
			Button buttonUpperEast = FindViewById<Button> (Resource.Id.buttonUpperEast);

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

			try {
				await _server.StartSubscriptionsAsync(new PremiseTcpClientSocket());
				_server.FastMode = true;

				AddGDO("sys://Home/Upstairs/Garage/West Garage Door", textLowerWest, textLowerWestStatus, buttonLowerWest);
				AddGDO("sys://Home/Upstairs/Garage/East Garage Door", textLowerEast, textLowerEastStatus, buttonLowerEast);
				AddGDO("sys://Home/Upper Garage/West Garage Door", textUpperWest, textUpperWestStatus, buttonUpperWest);
				AddGDO("sys://Home/Upper Garage/Center Garage Door", textUpperCenter, textUpperCenterStatus, buttonUpperCenter);
				AddGDO("sys://Home/Upper Garage/East Garage Door", textUpperEast, textUpperEastStatus, buttonUpperEast);

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
		}

		private async void AddGDO(string location, TextView textDesc, TextView textStatus, Button button){
			try {
				button.Enabled = false;
				PremiseObject ob = new PremiseObject(location);
				ob.PropertyChanged += (sender, args) => {
					button.Enabled = true;
					var propName = PremiseProperty.NameFromItem(args.PropertyName);
					var val = ((PremiseObject)sender)[propName];
					Console.WriteLine("{0}: {1} = {2}", ((PremiseObject)sender).Location, propName, val);

					switch (propName){
						case "GarageDoorStatus":
							textStatus.Text = val.ToString();
						break;
						case "Description":
							textDesc.Text = val.ToString();
						break;
						case "GarageDoorOpened":
							button.Text = (bool)val == true ? "Close" : "Open";
						break;
					}

				};

				await ob.AddPropertyAsync("Description");
				await ob.AddPropertyAsync("Trigger", PremiseProperty.PremiseType.TypeBoolean, false);
				await ob.AddPropertyAsync("GarageDoorOpened", PremiseProperty.PremiseType.TypeBoolean, true);
				await ob.AddPropertyAsync("GarageDoorStatus", PremiseProperty.PremiseType.TypeText, true);
				button.Click += (sender, e) => { 
					((dynamic)ob["TriggerCommand"]).Execute(null); 
				};
			} catch (Exception ex) {
				throw ex;
			}

		}
	}
}


