using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using PremiseWebClient;
using PremiseWP.Resources;

namespace PremiseWP.ViewModels {
    public class MainViewModel : INotifyPropertyChanged {
        public MainViewModel() {
            Server.PropertyChanged += (sender, args) => {
                if (args.PropertyName == "Connected") {
                    Debug.WriteLine("Connected: {0}", Server.Connected);
                    if (PremiseServer.Instance.Connected) {
                        Home = new PremiseObject("sys://Home");
                        Home.AddPropertyAsync("DisplayName", PremiseProperty.PremiseType.TypeText, true);
                    }
                    else {
                        
                    }
                }
                Connected = PremiseServer.Instance.Connected;
            };
            this.Items = new ObservableCollection<PremiseObject>();
        }

        /// <summary>
        /// A collection for ItemViewModel objects.
        /// </summary>
        public ObservableCollection<PremiseObject> Items { get; private set; }

        public bool Connected {
            get { return Server.Connected;  }
            set {
                NotifyPropertyChanged("Connected");
            }
        }

        public PremiseServer Server {
            get { return PremiseServer.Instance; }
            set {
                NotifyPropertyChanged("Server");
            }
        }

        private PremiseObject _home;
        public PremiseObject Home {
            get { return _home; }
            set {
                _home = value;
                NotifyPropertyChanged("Home");
            }
        }

        /// <summary>
        /// Creates and adds a few ItemViewModel objects into the Items collection.
        /// </summary>
        public async void LoadData() {
            try {
                Items.Clear();

                //PremiseObject o = new PremiseObject("sys://Home/Upstairs/Garage/West Garage Door");
                //await o.AddPropertyAsync("Description", PremiseProperty.PremiseType.TypeText);
                //await o.AddPropertyAsync("GarageDoorStatus", PremiseProperty.PremiseType.TypeText, true);
                //await o.AddPropertyAsync("Trigger", PremiseProperty.PremiseType.TypeBoolean);
                //this.Items.Add(o);

                //o = new PremiseObject("sys://Home/Upstairs/Garage/East Garage Door");
                //await o.AddPropertyAsync("Description", PremiseProperty.PremiseType.TypeText);
                //await o.AddPropertyAsync("GarageDoorStatus", PremiseProperty.PremiseType.TypeText, true);
                //await o.AddPropertyAsync("Trigger", PremiseProperty.PremiseType.TypeBoolean);
                //this.Items.Add(o);

                //o = new PremiseObject("sys://Home/Upper Garage/West Garage Door");
                //await o.AddPropertyAsync("Description", PremiseProperty.PremiseType.TypeText);
                //await o.AddPropertyAsync("GarageDoorStatus", PremiseProperty.PremiseType.TypeText, true);
                //await o.AddPropertyAsync("Trigger", PremiseProperty.PremiseType.TypeBoolean);
                //this.Items.Add(o);

                //o = new PremiseObject("sys://Home/Upper Garage/Center Garage Door");
                //await o.AddPropertyAsync("Description", PremiseProperty.PremiseType.TypeText);
                //await o.AddPropertyAsync("GarageDoorStatus", PremiseProperty.PremiseType.TypeText, true);
                //await o.AddPropertyAsync("Trigger", PremiseProperty.PremiseType.TypeBoolean);
                //this.Items.Add(o);

                //o = new PremiseObject("sys://Home/Upper Garage/East Garage Door");
                //await o.AddPropertyAsync("Description", PremiseProperty.PremiseType.TypeText);
                //await o.AddPropertyAsync("GarageDoorStatus", PremiseProperty.PremiseType.TypeText, true);
                //await o.AddPropertyAsync("Trigger", PremiseProperty.PremiseType.TypeBoolean);
                //this.Items.Add(o);

                //((ICommand)o2["TriggerCommand"]).Execute(null);
            }
            catch (Exception ex) {
                Debug.WriteLine("MainViewModel.LoadData: " + ex.Message);
            }
        }

        public async void Disconnect() {
            try {
                Server.StopSubscriptions();
            } catch (Exception ex) {
                Debug.WriteLine("MainViewModel.LoadData: " + ex.Message);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler) {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}