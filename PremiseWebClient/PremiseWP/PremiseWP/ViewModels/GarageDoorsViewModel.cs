using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using PremiseWP.Annotations;
using PremiseWebClient;

namespace PremiseWP.ViewModels {
    public class GarageDoorsViewModel : INotifyPropertyChanged {
        public GarageDoorsViewModel() {
            ApplicationTitle = "Kindel Residence";
            PageName = "Garage Doors";
            GDOItems = new ObservableCollection<PremiseObject>();
        }

        public String ApplicationTitle { get; set; }
        public String PageName { get; set; }

        /// <summary>
        /// A collection for ItemViewModel objects.
        /// </summary>
        public ObservableCollection<PremiseObject> GDOItems { get; private set; }

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

        private PremiseObject _LightsOff;
        public PremiseObject LightsOff
        {
            get { return _LightsOff; }
            set
            {
                _LightsOff = value;
                NotifyPropertyChanged("LightsOff");
            }
        }

        private PremiseObject _GDOPower;
        public PremiseObject GDOPower
        {
            get { return _GDOPower; }
            set
            {
                _GDOPower = value;
                NotifyPropertyChanged("GDOPower");
            }
        }
        /// <summary>
        /// Creates and adds a few ItemViewModel objects into the Items collection.
        /// </summary>
        public async void LoadData() {
            if (PremiseServer.Instance.Connected && GDOItems.Count > 0) return;

            IsDataLoaded = false;
            GDOItems.Clear();
            await PremiseServer.Instance.StartSubscriptionsAsync(new StreamSocketPremiseSocket());
            var o = new PremiseObject("sys://Home/Upstairs/Garage/East Garage Door");
            o.AddPropertyAsync("Description", "Lower East Garage", PremiseProperty.PremiseType.TypeText);
            o.AddPropertyAsync("GarageDoorOpened", PremiseProperty.PremiseType.TypeBoolean, true);
            o.AddPropertyAsync("Trigger", PremiseProperty.PremiseType.TypeBoolean);
            GDOItems.Add(o);

            o = new PremiseObject("sys://Home/Upstairs/Garage/West Garage Door");
            o.AddPropertyAsync("Description", "Lower West Garage", PremiseProperty.PremiseType.TypeText);
            o.AddPropertyAsync("GarageDoorOpened", PremiseProperty.PremiseType.TypeBoolean, true);
            o.AddPropertyAsync("Trigger", PremiseProperty.PremiseType.TypeBoolean);
            GDOItems.Add(o);

            o = new PremiseObject("sys://Home/Upper Garage/East Garage Door");
            o.AddPropertyAsync("Description", "Upper East Garage", PremiseProperty.PremiseType.TypeText);
            o.AddPropertyAsync("GarageDoorOpened", PremiseProperty.PremiseType.TypeBoolean, true);
            o.AddPropertyAsync("Trigger", PremiseProperty.PremiseType.TypeBoolean);
            GDOItems.Add(o);

            o = new PremiseObject("sys://Home/Upper Garage/Center Garage Door");
            o.AddPropertyAsync("Description", "Upper Center Garage", PremiseProperty.PremiseType.TypeText);
            o.AddPropertyAsync("GarageDoorOpened", PremiseProperty.PremiseType.TypeBoolean, true);
            o.AddPropertyAsync("Trigger", PremiseProperty.PremiseType.TypeBoolean);
            GDOItems.Add(o);

            o = new PremiseObject("sys://Home/Upper Garage/West Garage Door");
            o.AddPropertyAsync("Description", "Upper West Garage", PremiseProperty.PremiseType.TypeText);
            o.AddPropertyAsync("GarageDoorOpened", PremiseProperty.PremiseType.TypeBoolean, true);
            o.AddPropertyAsync("Trigger", PremiseProperty.PremiseType.TypeBoolean);
            GDOItems.Add(o);

            LightsOff = new PremiseObject("sys://Home/Admin/All House Scenes/Upper Garage Off");
            LightsOff.AddPropertyAsync("DisplayName", "Upper Garage Lights Off", PremiseProperty.PremiseType.TypeText, true);
            LightsOff.AddPropertyAsync("Status", PremiseProperty.PremiseType.TypeBoolean, true);
            LightsOff.AddPropertyAsync("Trigger", PremiseProperty.PremiseType.TypeBoolean);

            GDOPower = new PremiseObject("sys://Home/Admin/Garage Door Power");
            GDOPower.AddPropertyAsync("DisplayName", "GDO Power", PremiseProperty.PremiseType.TypeText, true); 
            GDOPower.AddPropertyAsync("State", PremiseProperty.PremiseType.TypeBoolean, true);

            IsDataLoaded = true;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler) {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class GDOStateConverter : IValueConverter {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value != null) {
                return (bool) value ? "Open" : "Closed";
            }
            return "No status";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }

        #endregion
    }

    public class GDOOpenCloseCommandConverter : IValueConverter {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value != null) {
                return (bool)value ? "Close" : "Open";
            }
            return "n/a";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }

        #endregion
    }

    public class GDOStateColorConverter : IValueConverter {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value != null) {
                return (bool) value ? "Red" : "GreenYellow";
            }
            return "Gray";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }

        #endregion
    }

    public class PremisePropertyHasValueConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return (value != null);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
    public class LightingButtonStatusConverter : IValueConverter {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value != null) 
                return (bool) value ? "GreenYellow" : "White";
            return "Gray";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }

        #endregion
    }

    public class SampleGarageDoors : INotifyPropertyChanged {
        public bool IsDataLoaded {
            get;
            set;
        }

        public PremiseObject GDOPower { get; set; }

        public bool ProgressIndicatorVisible { 
            get { return true; }
            set {}
        }
        public String ApplicationTitle { get; set; }
        public String PageName { get; set; }
        public ObservableCollection<PremiseObject> GDOItems { get; private set; }
        public PremiseObject LightsOff { get; private set; }
        public SampleGarageDoors() {
            ApplicationTitle = "Kindel Residence";
            PageName = "Garage Doors";
            IsDataLoaded = true;
            GDOPower = new PremiseObject("gdopower");
            GDOPower["DisplayName"] = "GDO Power!";
            GDOPower["State"] = true;

            GDOItems = new ObservableCollection<PremiseObject>();
            var o = new PremiseObject("lge");
            o["Description"] = "Lower Garage East";
            o["GarageDoorOpened"] = false;
            GDOItems.Add(o);
            o = new PremiseObject("lgw");
            o["Description"] = "Lower Garage West";
            o["GarageDoorOpened"] = true;
            GDOItems.Add(o);
            o = new PremiseObject("uge");
            o["Description"] = "Upper Garage East";
            o["GarageDoorOpened"] = true;
            GDOItems.Add(o);
            o = new PremiseObject("ugc");
            o["Description"] = "Upper Garage Center";
            o["GarageDoorOpened"] = true;
            GDOItems.Add(o);
            o = new PremiseObject("ugw");
            o["Description"] = "Upper Garage West";
            o["GarageDoorOpened"] = true;
            GDOItems.Add(o);

            LightsOff = new PremiseObject("lightsoff");
            LightsOff["DisplayName"] = "Upper Garage Off";
            LightsOff["Status"] = true;
        }
        public string Location { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    //public class Po : INotifyPropertyChanged {
    //    private Dictionary<string, Pp> _properties = new Dictionary<string, Pp>();
    //    public Dictionary<string, Pp> Properties {
    //        get { return _properties; }
    //        set { _properties = value; }
    //    }
    //    public object this[string name] {
    //        get {
    //            Pp prop;
    //            if (_properties.TryGetValue(name, out prop))
    //                return prop;

    //            return null;
    //        }
    //        set {
    //            Pp current = null;
    //            if (!_properties.TryGetValue(name, out current)) {
    //                current = new Pp { Name = name };
    //            }
    //            // Only update value if it changed
    //            if (current.Value == value.ToString()) return;
    //            current.Value = value;
    //            _properties[name] = current;
    //            OnPropertyChanged(name);
    //        }
    //    }

    //    public event PropertyChangedEventHandler PropertyChanged;

    //    [NotifyPropertyChangedInvocator]
    //    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
    //        PropertyChangedEventHandler handler = PropertyChanged;
    //        if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
    //    }
    //}

    //public class Pp : INotifyPropertyChanged {
    //    private object _value;
    //    public string Name { get; set; }

    //    public object Value {
    //        get { return _value; }
    //        set {
    //            if (value == null) {
    //                _value = null;
    //                return;
    //            }
    //            _value = Value;
    //        }
    //    }

    //    public event PropertyChangedEventHandler PropertyChanged;

    //    [NotifyPropertyChangedInvocator]
    //    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
    //        PropertyChangedEventHandler handler = PropertyChanged;
    //        if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
    //    }
    //}
}
