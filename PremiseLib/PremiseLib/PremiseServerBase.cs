// Copyright 2013 Kindel Systems
//   
//   This is the Command Line Version of PremiseServerBase

using System.ComponentModel;
using System.Runtime.CompilerServices;
using PremiseLib.Annotations;

namespace PremiseLib
{
    public class PremiseServerBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void DispatchSetMember(PremiseObject obj, string propertyName, string value) {
            //if (CoreApplication.MainView.CoreWindow.Dispatcher.HasThreadAccess)
                obj.SetMember(propertyName, value, false);
        //    else
        //        CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
        //        CoreDispatcherPriority.Normal, () => obj.SetMember(propertyName, value, false));
        }

        [NotifyPropertyChangedInvocator]
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                //if (CoreApplication.MainView.CoreWindow.Dispatcher.HasThreadAccess)
                    handler(this, new PropertyChangedEventArgs(propertyName));
                //else
                //    CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                //        CoreDispatcherPriority.Normal, () => handler(this, new PropertyChangedEventArgs(propertyName)));
            }
        }
    }
}