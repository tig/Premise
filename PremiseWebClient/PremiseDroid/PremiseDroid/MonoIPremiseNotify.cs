// Copyright 2013 Kindel Systems
//   
//   This is the Mono Version of PremiseServerBase

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using Android.App;
using PremiseWebClient;

namespace PremiseLib {
    public class MonoIPremiseNotify : Activity, IPremiseNotify {

        public void DispatchSetMember(PremiseObject obj, string propertyName, string value) {
			RunOnUiThread(() => obj.SetMember(propertyName, value, false));
		}

        public void OnPropertyChanged(PremiseServer thisServer, PropertyChangedEventHandler handler, [CallerMemberName] string propertyName = null) {
            if (handler != null) {
                    RunOnUiThread(() => handler(thisServer, new PropertyChangedEventArgs(propertyName)));
            }
        }
    }
}