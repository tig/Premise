// Generic .NET implementation of IPremiseNotify; assumes dispatching
// to UI thread is not needed.
//
using System.ComponentModel;
using System.Runtime.CompilerServices;
using PremiseLib;
using PremiseLib.Annotations;

namespace pr {
    class TestNotifier : IPremiseNotify {
        // Default implementation of method to set an object property's value
        // Assumes same thread.
        public void DispatchSetMember(PremiseObject obj, string propertyName, string value) {
            obj.SetMember(propertyName, value, false);
        }

        // Default OnPropertyChanged method assumes same thread.
        [NotifyPropertyChangedInvocator]
        public void OnPropertyChanged(PremiseServer thisServer, PropertyChangedEventHandler handler, [CallerMemberName] string propertyName = null) {
            if (handler != null) {
                handler(thisServer, new PropertyChangedEventArgs(propertyName));
            }
        }

    }
}
