using System;
using System.Windows.Input;

namespace PremiseWebClient {
    /// <summary>
    /// PremiseCommand supports XAML Commands. 
    /// For example:
    ///    CheckBox DataContext="{Binding GDOPower}" 
    ///    IsChecked="{Binding [State], Mode=TwoWay}" 
    ///    Command="{Binding [StateCommand]}" 
    ///    CommandParameter="{Binding Path=IsChecked, RelativeSource={RelativeSource Self}}"
    /// 
    /// and, as a Trigger:
    /// 
    ///   Button DataContext="{Binding}" 
    ///   Content="{Binding [GarageDoorOpened], Converter={StaticResource GDOOpenCloseCommandConverter}}" 
    ///   Command="{Binding [TriggerCommand]}"
    /// </summary>
    public class PremiseCommand : ICommand {
        private string _propertyName;
        private dynamic _holdingObject;

        public PremiseCommand(PremiseObject holdingObject, string propertyName) {
            _holdingObject = holdingObject;
            _propertyName = propertyName;
        }

        public string PropertyName {
            get { return _propertyName; }
            set {
                _propertyName = value;
                if (_holdingObject != null && !String.IsNullOrEmpty(_propertyName)) {
                    OnCanExecuteChanged();
                }
            }
        }

        public PremiseObject HoldingObject {
            get { return _holdingObject; }
            set {
                _holdingObject = value;
                if (_holdingObject != null && !String.IsNullOrEmpty(_propertyName)) {
                    OnCanExecuteChanged();
                }
            }
        }

        public bool CanExecute(object parameter) {
            return _holdingObject != null
                && !String.IsNullOrEmpty(_propertyName)
                && PremiseServer.Instance.Connected;
        }

        public void Execute(object parameter) {
            if (_holdingObject != null && !String.IsNullOrEmpty(_propertyName))
                // If there is no CommandParameter assume it is a Toggle property
                // and set it to true.
                _holdingObject.SetMember(_propertyName, parameter ?? true);
        }

        public event EventHandler CanExecuteChanged;
        protected virtual void OnCanExecuteChanged() {
            EventHandler handler = CanExecuteChanged;
            if (handler != null) handler(this, new EventArgs());
        }
    }
}
