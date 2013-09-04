// Copyright 2013 Kindel Systems
//   
//   This is the Command Line Version of PremiseServerBase

using System.ComponentModel;
using System.Runtime.CompilerServices;
using PremiseLib.Annotations;

namespace PremiseLib
{
    public sealed class PremiseServerBase 
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
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