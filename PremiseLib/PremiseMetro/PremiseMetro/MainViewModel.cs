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

            if (IsInDesignMode) return;

            PremiseServer.Instance.Host = "home";
            PremiseServer.Instance.Port = 86;
            PremiseServer.Instance.Username = "";
            PremiseServer.Instance.Password = "";
            PremiseServer.Instance.PropertyChanged += async (sender, args) => {
                if (args.PropertyName == "Connected" && PremiseServer.Instance.Connected == true) {
                    Debug.WriteLine("Yo! Connected!!");

                    PremiseObject o1 = new PremiseObject("sys://Home/Downstairs/Office/Undercounter");
                    //await o1.AddPropertyAsync("Description", PremiseProperty.PremiseType.TypeText);
                    await o1.AddPropertyAsync("Brightness", PremiseProperty.PremiseType.TypePercent, true);
                    //await PremiseServer.Instance.Subscribe(o1, "Brightness");
                    //await o1.AddPropertyAsync("PowerState", PremiseProperty.PremiseType.TypeText, true);

                    //PremiseObject o2 = new PremiseObject("sys://Home/Downstairs/Office/Uplighting");
                    //await o2.AddPropertyAsync("Description", PremiseProperty.PremiseType.TypeText);
                    //await o2.AddPropertyAsync("Brightness", PremiseProperty.PremiseType.TypePercent, true);
                    //await PremiseServer.Instance.Subscribe(o2, "Brightness");
                    //await o2.AddPropertyAsync("PowerState", PremiseProperty.PremiseType.TypeText, true);

                    Lights = new ObservableCollection<PremiseObject> {
                        (PremiseObject)o1
                    //    (PremiseObject)o2
                    };
                    foreach (PremiseObject l in Lights) {
                        l.PropertyChanged += (s, a) => Debug.WriteLine("MVM: {0}: {1} = {2}",
                                                                                      ((PremiseObject)s).Location,
                                                                                      a.PropertyName,
                                                                                      ((PremiseObject)s)[a.PropertyName]);
                    }

                }
                if (args.PropertyName == "Connected" && PremiseServer.Instance.Connected == false)
                    Debug.WriteLine("Disconnected!");
            };
            Task t = PremiseServer.Instance.StartSubscriptionsAsync();
             
            //Light light = new Light();
            //light["DisplayName"] = "Test Light";
            //light["Brightness"] = "27%";
            //light["PowerState"] = false;
            ////light.DisplayName = "Test Light";
            ////light.Brightness = "27%";
            ////light.PowerState = false;
            //Lights = new ObservableCollection<Light> { light };

            //dynamic light = new ExpandoObject();
            //light.DisplayName = "Test Light";
            //light.Brightness = "27%";
            //light.PowerState = false;
            //Lights = new ObservableCollection<dynamic> { light };
        }



        private ObservableCollection<PremiseObject> _lights;
        public ObservableCollection<PremiseObject> Lights {
            get { return _lights; }
            set { _lights = value; RaisePropertyChanged("Lights"); }
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