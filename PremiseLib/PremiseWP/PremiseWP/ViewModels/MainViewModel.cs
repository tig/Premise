using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;
using PremiseLib;
using PremiseWP.Resources;

namespace PremiseWP.ViewModels {
    public class MainViewModel : INotifyPropertyChanged {
        public MainViewModel() {
            this.Items = new ObservableCollection<PremiseObject>();
        }

        /// <summary>
        /// A collection for ItemViewModel objects.
        /// </summary>
        public ObservableCollection<PremiseObject> Items { get; private set; }

        public bool IsDataLoaded {
            get;
            private set;
        }

        /// <summary>
        /// Creates and adds a few ItemViewModel objects into the Items collection.
        /// </summary>
        public async void LoadData() {

            PremiseServer.Instance.Notifier = new WPIPremiseNotify();
            PremiseServer.Instance.Host = "home.kindel.net";
            PremiseServer.Instance.Port = 86;
            PremiseServer.Instance.Username = "";
            PremiseServer.Instance.Password = "";
            PremiseServer.Instance.PropertyChanged += async (sender, args) => {
                if (args.PropertyName == "Connected" && PremiseServer.Instance.Connected == true) {
                    Debug.WriteLine("Yo! Connected!!");
                }
                if (args.PropertyName == "Connected" && PremiseServer.Instance.Connected == false)
                    Debug.WriteLine("Disconnected!");
            };
            await PremiseServer.Instance.StartSubscriptionsAsync(new StreamSocketPremiseSocket());

            PremiseObject o1 = new PremiseObject("sys://Home/Downstairs/Office/Office At Entry Door/Button_Desk");
            await o1.AddPropertyAsync("Description", PremiseProperty.PremiseType.TypeText);
            await o1.AddPropertyAsync("Status", PremiseProperty.PremiseType.TypeBoolean, true);
            await o1.AddPropertyAsync("Trigger", PremiseProperty.PremiseType.TypeBoolean);
            this.Items.Add(o1);

            PremiseObject o2 = new PremiseObject("sys://Home/Downstairs/Office/Office At Entry Door/Button_Workshop");
            await o2.AddPropertyAsync("Description", PremiseProperty.PremiseType.TypeText);
            await o2.AddPropertyAsync("Status", PremiseProperty.PremiseType.TypeBoolean, true);
            await o2.AddPropertyAsync("Trigger", PremiseProperty.PremiseType.TypeBoolean);
            this.Items.Add(o2);

            ((ICommand)o2["TriggerCommand"]).Execute(null);

            this.IsDataLoaded = true;
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