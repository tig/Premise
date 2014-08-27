// Copyright 2013 Kindel Systems
//   
//   

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PremiseWebClient {
    /// <summary>
    ///     A local representation of an object on the Premise server.
    /// </summary>
    public class PremiseObject : INotifyPropertyChanged {
        public string Location { get; set; }

        private bool _hasServerData;
        /// <summary>
        /// If any of the [properties] on the object were updated from
        /// the server this will be set to true.
        /// </summary>
        public bool HasServerData {
            get { return _hasServerData; }
            set {
                _hasServerData = value;
                OnPropertyChanged("HasServerData"); 
            } 
        }

        public PremiseObject(string location) {
            Location = location;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private readonly Dictionary<string, PremiseProperty> _properties = new Dictionary<string, PremiseProperty>();

        /// <summary>
        ///     Add a set of properties to the object. This is a helper function around 
        ///     AddPropertyAsync.
        /// </summary>
        /// <param name="subscribe">True to cause each property to be subscribed to</param>
        /// <param name="properties">The properties</param>
        /// <returns></returns>
        public async Task AddPropertiesAsync(bool subscribe = false, params PremiseProperty[] properties) {
            foreach (var property in properties) {
                await AddPropertyAsync(property.Name, property.PropertyType, subscribe);
            }
        }

        /// <summary>
        ///     Add a property to the object. Makes a call to the server to get the value.
        /// </summary>
        /// <param name="propertyName">Name of the property (e.g. foo["propertyname"])</param>
        /// <param name="type">Property type</param>
        /// <param name="subscribe">True to cause the property to be subscribed to</param>
        /// <returns></returns>
        public async Task AddPropertyAsync(string propertyName,
                                           PremiseProperty.PremiseType type = PremiseProperty.PremiseType.TypeText,
                                           bool subscribe = false) {
            try {
                PremiseProperty prop;
                if (!_properties.TryGetValue(propertyName, out prop)) {
                    _properties[propertyName] = new PremiseProperty(propertyName) { PropertyType = type };
                }

                if (subscribe) {
                    await PremiseServer.Instance.Subscribe(this, propertyName);
                }

                // Sadly, premise does not automatically send a update notification upon
                // a subscription. Thus even for properites where we are using a subscription
                // we have to block waiting for the initial value. 
                string val = await PremiseServer.Instance.GetValueTaskAsync(Location, propertyName);
                Debug.WriteLine("got {0} {1} = {2}", Location, propertyName, val);

                SetMember(propertyName, val, false);
                HasServerData = true;
            } catch (Exception) {
                throw;
            }
        }

        /// <summary>
        ///     Add a property to the object. Makes a call to the server to get the value if
        ///     initialValue is not specified or subscribe is set to true.
        /// </summary>
        /// <param name="propertyName">Name of the property (e.g. foo["propertyname"])</param>
        /// <param name="initalValue">Initial value for the property (enables reducing UI churn on first load)</param>
        /// <param name="type">Property type</param>
        /// <param name="subscribe">True to cause the property to be subscribed to</param>
        /// <returns></returns>
        public async Task AddPropertyAsync(string propertyName,
                                           object initalValue,
                                           PremiseProperty.PremiseType type = PremiseProperty.PremiseType.TypeText,
                                           bool subscribe = false) {
            PremiseProperty prop;
            if (!_properties.TryGetValue(propertyName, out prop)) {
                _properties[propertyName] = new PremiseProperty(propertyName) {PropertyType = type, Value = initalValue};
            }

            // Ignore server
            if (initalValue != null && subscribe == false) return;

            await AddPropertyAsync(propertyName, type, subscribe);
        }

        ///// <summary>
        /////     Add a command property to the object. Commands that are not also properties are boolean toggles, so there is
        /////     no need to request the state from the server. 
        ///// </summary>
        ///// <param name="commandName">Name of the command (e.g. "trigger" in foo["triggerCommand"])</param>
        ///// <returns></returns>
        //public void AddCommand(string commandName) {
        //    try {
        //        PremiseProperty prop;
        //        if (!_properties.TryGetValue(commandName, out prop)) {
        //            _properties[commandName] = new PremiseProperty(commandName, PremiseProperty.PremiseType.TypeBoolean);
        //        }
                
        //        // Make sure any subscribers get notified with the correct property name
        //        OnPropertyChanged(commandName + "Command");

        //    } catch (Exception ex) {
        //        throw;
        //    }
        //}

        /// <summary>
        /// Property accessor. Simulates a dynamic object.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public object this[string name] {
            get {
                PremiseProperty prop;
                if (_properties.TryGetValue(name, out prop))
                    return prop.Value;

                // In XAML <Button Content="Press Me" Command="{Binding [TriggerCommand]}">
                // where 'Trigger' is the name of the property that is momentary.
                // All Properties can be Commands.
                if (name.EndsWith("Command")) 
                    return new PremiseCommand(this, name.Substring(0, name.Length - "Command".Length));

                return null;
            }
            set {
                SetMember(name, value);
            }
        }

        //public async void SetMemberAsync(String propertyName, object value, bool sendToServer = true) {
        //    PremiseProperty current = null;
        //    if (!_properties.TryGetValue(propertyName, out current)) {
        //        current = new PremiseProperty(propertyName);
        //    }
        //    // Only update value if it changed
        //    if (current.Value == value) return;

        //    current.Value = value;
        //    _properties[propertyName] = current;
        //    OnPropertyChanged(propertyName);
        //    if (sendToServer) {
        //        var server = PremiseServer.Instance;
        //        if (server != null && !String.IsNullOrEmpty(Location)) {
        //            // Spin up a new thread for this to get it off the UI thread (not really needed, probably)
        //            server.SetValueAsync(Location, propertyName, value.ToString());
        //        }
        //    }
        //}

        public void SetMember(String propertyName, object value, bool sendToServer = true) {
            PremiseProperty current;
            if (!_properties.TryGetValue(propertyName, out current)) {
                current = new PremiseProperty(propertyName);
            }
            // Only update value if it changed
            if (current.Value == value) return;

            current.Value = value;
            _properties[propertyName] = current;
            OnPropertyChanged(propertyName);
            if (sendToServer) {
                Debug.WriteLine("Updating server: {0}: {1}", propertyName, value);
                SendPropertyChangeToServer(propertyName, value);
            }
        }

        private void SendPropertyChangeToServer(String propertyName, object value) {
            Debug.WriteLine("SendPropertyChangeToServer(\"{0}\", \"{1}\")", propertyName, value);
            var server = PremiseServer.Instance;
            if (server != null && !String.IsNullOrEmpty(Location)) {
                // Spin up a new thread for this to get it off the UI thread (not really needed, probably)
                Task.Factory.StartNew(() => server.SetValueAsync(Location, propertyName, value.ToString()));
            }
        }

        protected virtual void OnPropertyChanged(string propertyName) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler == null) return;

            // Special case real properties
            if (propertyName != "HasServerData") 
                propertyName = PremiseProperty.ItemFromName(propertyName);
            handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}