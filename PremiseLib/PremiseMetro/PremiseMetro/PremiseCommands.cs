using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PremiseLib {
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
            return _holdingObject != null && !String.IsNullOrEmpty(_propertyName);
        }

        public void Execute(object parameter) {
            if (_holdingObject != null && !String.IsNullOrEmpty(_propertyName))
                _holdingObject.SetMember(_propertyName, true);
        }

        public event EventHandler CanExecuteChanged;
        protected virtual void OnCanExecuteChanged() {
            EventHandler handler = CanExecuteChanged;
            if (handler != null) handler(this, new EventArgs());
        }
    }
}
