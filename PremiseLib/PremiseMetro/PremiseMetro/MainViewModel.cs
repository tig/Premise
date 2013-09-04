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

            //PremiseServer.Instance.Host = "home";
            //PremiseServer.Instance.Port = 86;
            //PremiseServer.Instance.Username = "";
            //PremiseServer.Instance.Password = "";
            //PremiseServer.Instance.PropertyChanged += async (sender, args) => {
            //    if (args.PropertyName == "Connected" && PremiseServer.Instance.Connected == true) {
            //        Debug.WriteLine("Yo! Connected!!");
            //        dynamic o1 = new PremiseObject("sys://Home/Downstairs/Office/Undercounter");
            //        await o1.AddPropertyAsync("DisplayName", PremiseProperty.PremiseType.TypeText);
            //        await o1.AddPropertyAsync("Brightness", PremiseProperty.PremiseType.TypePercent, false);
            //        await PremiseServer.Instance.Subscribe(o1, "Brightness");
            //        await o1.AddPropertyAsync("PowerState", PremiseProperty.PremiseType.TypeText, true);

            //        dynamic o2 = new PremiseObject("sys://Home/Downstairs/Office/Uplighting");
            //        await o2.AddPropertyAsync("DisplayName", PremiseProperty.PremiseType.TypeText);
            //        await o2.AddPropertyAsync("Brightness", PremiseProperty.PremiseType.TypePercent, false);
            //        await PremiseServer.Instance.Subscribe(o2, "Brightness");
            //        await o2.AddPropertyAsync("PowerState", PremiseProperty.PremiseType.TypeText, true);
                    
            //        o2.PowerState = true;
            //        GarageDoorOpeners = new ObservableCollection<dynamic> {
            //            o1,
            //            o2
            //        };
            //        foreach (PremiseObject garageDoorOpener in GarageDoorOpeners) {
            //            garageDoorOpener.PropertyChanged += (s, a) => Debug.WriteLine("MVM: {0}: {1} = {2}",
            //                                                                          ((PremiseObject)s).Location,
            //                                                                          a.PropertyName,
            //                                                                          ((PremiseObject)s).GetMember(a.PropertyName));
            //        }
                    
            //    }
            //    if (args.PropertyName == "Connected" && PremiseServer.Instance.Connected == false) 
            //        Debug.WriteLine("Disconnected!");
            //};
            //Task t = PremiseServer.Instance.StartSubscriptionsAsync();

            Light light = new Light();
            ((dynamic) light).DisplayName = "Test Light";
            ((dynamic) light).Brightness= "27%";
            ((dynamic) light).PowerState= false;
            Lights = new ObservableCollection<dynamic> {
                light
            };
        }

        
        private ObservableCollection<dynamic> _lights; 
        public ObservableCollection<dynamic> Lights {
            get { return _lights; }
            set { _lights = value; RaisePropertyChanged("Lights"); }
        }
    }

    class Light : DynamicObject, INotifyPropertyChanged {
        private readonly Dictionary<string, object> _properties = new Dictionary<string, object>();

        public event PropertyChangedEventHandler PropertyChanged;

        public override bool TryGetMember(GetMemberBinder binder, out object result) {
            string name = binder.Name;
            result = null;
            // If the property name is found in a dictionary, 
            // set the result parameter to the property value and return true. 
            // Otherwise, return false. 
            object prop;
            if (_properties.TryGetValue(name, out prop)) {
                result = prop;
                return true;
            }
            return false;
        }

        // If you try to set a value of a property that is 
        // not defined in the class, this method is called. 
        public override bool TrySetMember(SetMemberBinder binder, object value) {
            string name = binder.Name;

            _properties[name] = value;
            if (CoreApplication.MainView.CoreWindow.Dispatcher.HasThreadAccess)
                OnPropertyChanged(name);
            else
                CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal, () => OnPropertyChanged(name));

            // You can always add a value to a dictionary, 
            // so this method always returns true. 
            return true;
        }

        public object GetMember(string propName) {
            var binder = Binder.GetMember(CSharpBinderFlags.None,
                                          propName, GetType(),
                                          new List<CSharpArgumentInfo> {
                                              CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                                          });
            var callsite = CallSite<Func<CallSite, object, object>>.Create(binder);

            return callsite.Target(callsite, this);
        }

        /// <summary>
        ///     Sets the value of a property.
        /// </summary>
        /// <param name="propertyName">Name of property</param>
        /// <param name="val">New value</param>
        /// <param name="fromServer">If true, will not try to update server.</param>
        public void SetMember(String propertyName, object val) {
            var binder = Binder.SetMember(CSharpBinderFlags.None,
                                          propertyName, GetType(),
                                          new List<CSharpArgumentInfo> {
                                              CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                                              CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                                          });
            var callsite = CallSite<Func<CallSite, object, object, object>>.Create(binder);

            callsite.Target(callsite, this, val);
        }

        protected virtual void OnPropertyChanged(string propertyName) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }            
    }
}