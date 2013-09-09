// Copyright 2013 Kindel Systems
//   
//   This is the WinRT Version of PremiseServerBase

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace PremiseLib {
    public class WPIPremiseNotify : IPremiseNotify {

        public void DispatchSetMember(PremiseObject obj, string propertyName, string value) {
            if (Deployment.Current.Dispatcher.CheckAccess())
                obj.SetMember(propertyName, value, false);
            else
                Deployment.Current.Dispatcher.BeginInvoke(() => obj.SetMember(propertyName, value, false));
        }

        public void OnPropertyChanged(PremiseServer thisServer, PropertyChangedEventHandler handler, [CallerMemberName] string propertyName = null) {
            if (handler != null) {
                if (Deployment.Current.Dispatcher.CheckAccess())
                    handler(thisServer, new PropertyChangedEventArgs(propertyName));
                else
                    Deployment.Current.Dispatcher.BeginInvoke(() => handler(thisServer, new PropertyChangedEventArgs(propertyName)));
            }
        }
    }
}