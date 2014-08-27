using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using PremiseWP.Annotations;
using PremiseWebClient;

namespace PremiseWP.ViewModels {
    public class LightsViewModel : INotifyPropertyChanged {
        public LightsViewModel() {
            ApplicationTitle = "Kindel Residence";
            PageName = "Lights";
            Lights = new ObservableCollection<PremiseObject>();
        }

        public String ApplicationTitle { get; set; }
        public String PageName { get; set; }

        /// <summary>
        /// A collection for ItemViewModel objects.
        /// </summary>
        public ObservableCollection<PremiseObject> Lights { get; private set; }
        public PremiseObject LightsOff { get; private set; }

        private bool _isDataLoaded;
        public bool IsDataLoaded {
            get { return _isDataLoaded; }
            set { 
                _isDataLoaded = value;
                NotifyPropertyChanged("IsDataLoaded");
                NotifyPropertyChanged("ProgressIndicatorVisible");
            }
        }

        public bool ProgressIndicatorVisible {
            get { return !_isDataLoaded; }
            set { }
        }
        /// <summary>
        /// Creates and adds a few ItemViewModel objects into the Items collection.
        /// </summary>
        public async void LoadData() {
            if (Lights.Count > 0) return;

            string lights = await PremiseServer.Instance.InvokeMethodTaskAsync("sys://Home/Admin/WebClientMethods", "GetLights(\"sys://Home\")");
            Debug.WriteLine("Result: {0}", lights);

            // <-- this is the whole cause of this confusing architecture
            foreach (string location in lights.Split(',').Where(location => location.Length > 0))
                AddLight(location);

            //AddLight("sys://Home/Downstairs/Office/Surface");
            //AddLight("sys://Home/Downstairs/Office/Undercounter");
            //AddLight("sys://Home/Downstairs/Office/Uplighting");
            //AddLight("sys://Home/Downstairs/Office/Desk");
            //AddLight("sys://Home/Downstairs/Office/Equipment Room/Fluorescents");
            //AddLight("sys://Home/Downstairs/Office/Workshop/Undercounter");
            //AddLight("sys://Home/Downstairs/Office/Workshop/Fluorescents");
            this.IsDataLoaded = true;
        }

        private void AddLight(string location) {
            var o = new PremiseObject(location);
            o.AddPropertyAsync("Name", PremiseProperty.PremiseType.TypeText);
            o.AddPropertyAsync("Description",  PremiseProperty.PremiseType.TypeText);
            o.AddPropertyAsync("PowerState", PremiseProperty.PremiseType.TypeBoolean, true);
            if (!location.Contains("Fluorescent") && !location.Contains("Flourescent") && !location.Contains("Fan"))
                o.AddPropertyAsync("Brightness", PremiseProperty.PremiseType.TypePercent, true);
            Lights.Add(o);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler) {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class BrightnessConverter : IValueConverter {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is double) return value;

            var s1 = value as string;
            if (s1 != null) {
                double f;
                if (double.TryParse(s1, out f))
                    return f;
            }
            return 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return value;
        }

        #endregion
    }

    public class SampleLights : INotifyPropertyChanged {
        public bool IsDataLoaded {
            get;
            set;
        }

        public bool ProgressIndicatorVisible { 
            get { return true; }
            set {}
        }
        public String ApplicationTitle { get; set; }
        public String PageName { get; set; }
        public ObservableCollection<PremiseObject> Lights { get; private set; }
        public PremiseObject LightsOff { get; private set; }
        public SampleLights() {
            ApplicationTitle = "Premise Demo";
            PageName = "Lights";
            IsDataLoaded = true;
            Lights = new ObservableCollection<PremiseObject>();

            for (int i = 1 ; i < 10 ; i++)
                AddLight("Light Number " + i);
        }

        public void AddLight(string name) {
            var o = new PremiseObject(name);
            o["Name"] = name;
            o["Description"] = name + " descripton";
            o["PowerState"] = false;
            o["Brightness"] = 0.75;
            Lights.Add(o);
        }

        public string Location { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
