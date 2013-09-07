using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PremiseLib
{
    class PremiseCommand : ICommand {
        private string _propertyName;
        private dynamic _holdingObject;

        public PremiseCommand(PremiseObject holdingObject, string propertyName) {
            _holdingObject = holdingObject;
            _propertyName = propertyName;
        }
        public bool CanExecute(object parameter) {
            return _holdingObject != null;
        }

        public void Execute(object parameter) {
            if (_holdingObject)
                _holdingObject.SetMember(_propertyName, true);
        }

        public event EventHandler CanExecuteChanged;
    }
}
