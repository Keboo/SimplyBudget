namespace SimplyBudget.ViewModels
{
    internal class ErrorProperty<T> : ViewModelBase
    {
        private T _value;
        public T Value
        {
            get { return _value; }
            set
            {
                if (SetProperty(ref _value, value))
                    ClearError();
            }
        }

        private bool _isError;
        public bool IsError
        {
            get { return _isError; }
        }

        private string _message;
        public string Message
        {
            get { return _message; }
        }

        public void ClearError()
        {
            _message = null;
            _isError = false;
            RaisePropertyChanged(() => IsError);
            RaisePropertyChanged(() => Message);
        }

        public void SetError(string errorMessage)
        {
            _message = errorMessage;
            _isError = true;
            RaisePropertyChanged(() => IsError);
            RaisePropertyChanged(() => Message);
        }
    }
}