// Copyright 2013 Kindel Systems
//   
//   

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.CSharp.RuntimeBinder;

namespace PremiseLib {
    /// <summary>
    ///     A local representation of an object on the Premise server.
    /// </summary>
    public class PremiseObject : DynamicObject, INotifyPropertyChanged {
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
            _properties[propertyName] = new PremiseProperty(propertyName, type);
            Console.WriteLine("getting {0} {1}", Location, propertyName);
            SetMember(propertyName, await PremiseServer.Instance.GetValueTaskAsync(Location, propertyName), true);
            if (subscribe)
                PremiseServer.Instance.Subscribe(this, propertyName);
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result) {
            string name = binder.Name;
            result = null;
            // If the property name is found in a dictionary, 
            // set the result parameter to the property value and return true. 
            // Otherwise, return false. 
            PremiseProperty prop;
            if (_properties.TryGetValue(name, out prop)) {
                result = prop.Value;
                return true;
            }
            return false;
        }

        // If you try to set a value of a property that is 
        // not defined in the class, this method is called. 
        public override bool TrySetMember(SetMemberBinder binder, object value) {
            string name = binder.Name;

            PremiseProperty current = null;
            // If this is a new property, add it to the dictionary and assume it's text
            if (!_properties.TryGetValue(name, out current)) {
                current = new PremiseProperty(name, PremiseProperty.PremiseType.TypeText);
            }
            // Only update value if it changed
            if (current.Value == value) return true;

            bool fromServer = (current.Value == null);
            current.Value = value;
            _properties[name] = current;
            if (value != null && !fromServer) {
                Console.WriteLine("Updating server: {0}: {1}", name, value);
                SendPropertyChangeToServer(name, value);
            }
            OnPropertyChanged(name);

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
        public void SetMember(String propertyName, object val, bool fromServer = false) {
            //Console.WriteLine("SetMember fromServer = {0}: {1} = {2}", fromServer, propertyName, val);
            if (fromServer)
                _properties[propertyName].Value = null; // this prevents sending back to server

            var binder = Binder.SetMember(CSharpBinderFlags.None,
                                          propertyName, GetType(),
                                          new List<CSharpArgumentInfo> {
                                              CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                                              CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                                          });
            var callsite = CallSite<Func<CallSite, object, object, object>>.Create(binder);

            callsite.Target(callsite, this, val);
        }

        private void SendPropertyChangeToServer(String propertyName, object value) {
            Console.WriteLine("SendPropertyChangeToServer(\"{0}\", \"{1}\")", propertyName, value);
            var server = PremiseServer.Instance;
            if (server != null && !String.IsNullOrEmpty(Location))
                server.SetValue(Location, propertyName, value.ToString());
        }

        protected virtual void OnPropertyChanged(string propertyName) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}