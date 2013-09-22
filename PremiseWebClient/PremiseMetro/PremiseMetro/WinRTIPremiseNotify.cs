// Copyright 2013 Kindel Systems
//   
//   This is the WinRT Version of PremiseServerBase

using System.ComponentModel;
using System.Runtime.CompilerServices;
using PremiseLib.Annotations;
using PremiseWebClient;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace PremiseMetro {
    public class WinRTIPremiseNotify : IPremiseNotify {

        public void DispatchSetMember(PremiseObject obj, string propertyName, string value) {
            if (CoreApplication.MainView.CoreWindow.Dispatcher.HasThreadAccess)
                obj.SetMember(propertyName, value, false);
            else   
                CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal, () => obj.SetMember(propertyName, value, false));
        }

        [NotifyPropertyChangedInvocator]
        public void OnPropertyChanged(PremiseServer thisServer, PropertyChangedEventHandler handler, [CallerMemberName] string propertyName = null) {
            if (handler != null) {
                if (CoreApplication.MainView.CoreWindow.Dispatcher.HasThreadAccess)
                    handler(thisServer, new PropertyChangedEventArgs(propertyName));
                else
                    CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal, () => handler(thisServer, new PropertyChangedEventArgs(propertyName)));
            }
        }
    }
}