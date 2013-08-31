using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Dynamic;
using Microsoft.CSharp.RuntimeBinder;

namespace pr
{
    public class PremiseObject : DynamicObject, INotifyPropertyChanged{

        private Dictionary<string, object> _properties = new Dictionary<string, object>();

        // This property returns the number of elements 
        // in the inner dictionary. 
        public int Count {
            get{ return _properties.Count; }
        }

        public String Location;

        private Guid _guid = Guid.NewGuid();

        public PremiseObject() {
        }

        public async Task<int> Init(string location, params string[] properties) {
            Location = location;
            int n = 0;
            var server = PremiseServer.Instance;
            if (server != null && !String.IsNullOrEmpty(this.Location))
            {
                foreach (var property in properties) {
                    SetMember(property, await server.GetPropertyAsync(this.Location, property));
                    n++;
                }
            }
            return n;
        }

        public void SubscribeToProperty(string property) {
            var server = PremiseServer.Instance;
            if (server != null && !String.IsNullOrEmpty(this.Location)) {
                server.SubscribeToProperty(Location, property, _guid.ToString(), (id, value) => {
                    Debug.WriteLine("Property Update: Object {0} {1} = {2}", id, property, value);
                    _properties[property] = null; // this prevents sending back to server
                    SetMember(property, value);
                });
            }
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result){
            string name = binder.Name;

            // If the property name is found in a dictionary, 
            // set the result parameter to the property value and return true. 
            // Otherwise, return false. 
            if (!_properties.TryGetValue(name, out result)) {
                result = "(empty)";
            }
            return true;
        }


        // If you try to set a value of a property that is 
        // not defined in the class, this method is called. 
        public override bool TrySetMember(SetMemberBinder binder, object value) {
            string name = binder.Name;

            // Only update value if it changed
            object current = null;
            if (!_properties.TryGetValue(name, out current) || current != value) {
                _properties[name] = value;
                if (current != null) {
                    Console.WriteLine("Updating server: {0}: {1}", name, value);
                    SendPropertyChangeToServer(name, value);
                }
                OnPropertyChanged(name);
            }

            // You can always add a value to a dictionary, 
            // so this method always returns true. 
            return true;
        }

        public object GetMember(string propName)
        {
            var binder = Binder.GetMember(CSharpBinderFlags.None,
                  propName, this.GetType(),
                  new List<CSharpArgumentInfo>{
                       CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)});
            var callsite = CallSite<Func<CallSite, object, object>>.Create(binder);

            return callsite.Target(callsite, this);
        }

        public void SetMember(string propName, object val) {
            var binder = Binder.SetMember(CSharpBinderFlags.None,
                   propName, this.GetType(),
                   new List<CSharpArgumentInfo>{
                       CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                       CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)});
            var callsite = CallSite<Func<CallSite, object, object, object>>.Create(binder);

            callsite.Target(callsite, this, val);
        }

        private void SendPropertyChangeToServer(String name, object value){
            Debug.WriteLine("SendPropertyChangeToServer(\"{0}\", \"{1}\")", name, value);
            var server = PremiseServer.Instance;
            if (server != null && !String.IsNullOrEmpty(this.Location))
                server.SetPropertyAsync(this.Location, name, value.ToString());
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
