// Copyright 2013 Kindel Systems
//   
//   

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.CSharp.RuntimeBinder;

namespace PremiseLib {
    /// <summary>
    ///     A local representation of an object on the Premise server.
    /// </summary>
    public class PremiseObject : INotifyPropertyChanged {
        private readonly Dictionary<string, PremiseProperty> _properties = new Dictionary<string, PremiseProperty>();
        public String Location;

        public PremiseObject(string location) {
            Location = location;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        ///     Add a set of properties to the object.
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
        ///     Add a property to the object.
        /// </summary>
        /// <param name="propertyName">Name of the object</param>
        /// <param name="type">Property type</param>
        /// <param name="subscribe">True to cause the property to be subscribed to</param>
        /// <returns></returns>
        public async Task AddPropertyAsync(string propertyName,
                                           PremiseProperty.PremiseType type = PremiseProperty.PremiseType.TypeText,
                                           bool subscribe = false) {
            try {
                _properties[propertyName] = new PremiseProperty(propertyName, type);

                Debug.WriteLine("getting {0} {1}", Location, propertyName);
                this.SetMember(propertyName, await PremiseServer.Instance.GetValueTaskAsync(Location, propertyName), false);
                if (subscribe)
                    // Do we really want to await here?
                    await PremiseServer.Instance.Subscribe(this, propertyName);
            } catch (Exception ex) {
                throw ex;
            }
        }

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

                // In XAML <Button Content="Trigger" Command="{Binding [TriggerCommand]}">
                // where 'Trigger' is the name of hte property that is momentary
                if (name.EndsWith("Command")) {
                    string cmd = name.Substring(0, name.Length - "Command".Length);
                    if (_properties.TryGetValue(cmd, out prop)) {
                        return new PremiseCommand(this, cmd);
                    }
                }
                return null;
            }
            set {
                SetMember(name, value);
            }
        }

        public void SetMember(String propertyName, object value, bool fromUI = true) {
            PremiseProperty current = null;
            if (!_properties.TryGetValue(propertyName, out current)) {
                current = new PremiseProperty(propertyName, PremiseProperty.PremiseType.TypeText);
            }
            // Only update value if it changed
            if (current.Value == value) return;

            current.Value = value;
            _properties[propertyName] = current;
            OnPropertyChanged(propertyName);
            if (fromUI) {
                Debug.WriteLine("Updating server: {0}: {1}", propertyName, value);
                SendPropertyChangeToServer(propertyName, value);
            }
        }

        private void SendPropertyChangeToServer(String propertyName, object value) {
            Debug.WriteLine("SendPropertyChangeToServer(\"{0}\", \"{1}\")", propertyName, value);
            var server = PremiseServer.Instance;
            if (server != null && !String.IsNullOrEmpty(Location))
                server.SetValue(Location, propertyName, value.ToString());
        }

        protected virtual void OnPropertyChanged(string propertyName) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs("Item[" + propertyName+"]"));
        }
    }
}