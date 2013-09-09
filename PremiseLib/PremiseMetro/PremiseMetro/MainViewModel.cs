using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Threading;
using Microsoft.CSharp.RuntimeBinder;
using PremiseLib;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace PremiseMetro {
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase {
        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel() {
            Debug.WriteLine("MainViewModel()");
            Task t = Connect();
        }

        private async Task Connect() {
            PremiseServer.Instance.Notifier = new WinRTIPremiseNotify();
            PremiseServer.Instance.Host = "home";
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

            if (IsInDesignMode) {
                KeypadButtons = new ObservableCollection<PremiseObject>();
                var o = new PremiseObject("test");
                o["Description"] = "Button 1";
                o["Status"] = false;
                o["Trigger"] = false;
                KeypadButtons.Add(o);
                o["Description"] = "Button 2";
                o["Status"] = true;
                o["Trigger"] = false;
                KeypadButtons.Add(o);

                return;
            }

            await PremiseServer.Instance.StartSubscriptionsAsync(new StreamSocketPremiseSocket());

            PremiseObject o1 = new PremiseObject("sys://Home/Downstairs/Office/Office At Entry Door/Button_Desk");
            await o1.AddPropertyAsync("Description", PremiseProperty.PremiseType.TypeText);
            await o1.AddPropertyAsync("Status", PremiseProperty.PremiseType.TypeBoolean, true);
            await o1.AddPropertyAsync("Trigger", PremiseProperty.PremiseType.TypeBoolean);

            PremiseObject o2 = new PremiseObject("sys://Home/Downstairs/Office/Office At Entry Door/Button_Workshop");
            await o2.AddPropertyAsync("Description", PremiseProperty.PremiseType.TypeText);
            await o2.AddPropertyAsync("Status", PremiseProperty.PremiseType.TypeBoolean, true);
            await o2.AddPropertyAsync("Trigger", PremiseProperty.PremiseType.TypeBoolean);

            KeypadButtons = new ObservableCollection<PremiseObject> {
                (PremiseObject) o1,
                (PremiseObject) o2
            };
            foreach (PremiseObject l in KeypadButtons) {
                l.PropertyChanged += (s, a) => Debug.WriteLine("MVM: {0}: {1} = {2}",
                                                               ((PremiseObject)s).Location,
                                                               PremiseProperty.NameFromItem(a.PropertyName),
                                                               ((PremiseObject)s)[PremiseProperty.NameFromItem(a.PropertyName)]);

            }
        }

        private ObservableCollection<PremiseObject> _KeypadButtons;
        public ObservableCollection<PremiseObject> KeypadButtons {
            get { return _KeypadButtons; }
            set { _KeypadButtons = value; RaisePropertyChanged("KeypadButtons"); }
        }
    }
    //public class Light {
    //    //public string DisplayName { get; set; }
    //    //public string Brightness { get; set; }
    //    //public bool PowerState { get; set; }

    //    public object this[string name] {
    //        get {
    //            //PremiseProperty prop;
    //            //if (_properties.TryGetValue(name, out prop))
    //            //    return prop.Value;
    //            return "hello";
    //        }
    //        set {
    //            //SetMember(name, value);
    //        }
    //    }

    //    public event PropertyChangedEventHandler PropertyChanged;

    //    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
    //        PropertyChangedEventHandler handler = PropertyChanged;
    //        if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
    //    }
    //}
}