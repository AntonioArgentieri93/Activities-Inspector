using GalaSoft.MvvmLight;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace ProgettoInformaticaForense_Argentieri.ViewModels
{
    public abstract class ValidationViewModelBase : ViewModelBase, INotifyDataErrorInfo
    {
        private Dictionary<string, List<string>> _errors = new Dictionary<string, List<string>>();

        public const string InvalidData = "Invalid data";

        public bool HasErrors
        {
            get
            {
                return _errors.Any();
            }
        }

        public IEnumerable GetErrors(string propertyName)
        {
            if ((string.IsNullOrEmpty(propertyName) || !_errors.ContainsKey(propertyName)))
                return null;

            return _errors[propertyName];
        }

        protected void AddError(string propertyName, string error = InvalidData)
        {
            this._errors[propertyName] = new List<string>() { error };
            RaiseErrorsChanged(propertyName);
        }

        protected void RemoveError(string propertyName)
        {
            if (this._errors.ContainsKey(propertyName))
                this._errors.Remove(propertyName);

            RaiseErrorsChanged(propertyName);
        }

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        private void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        protected void Validate(Func<bool> check, string propertyName)
        {
            if (check())
            {
                RemoveError(propertyName);
            }
            else
            {
                AddError(propertyName);
            }
        }
    }
}
