namespace SimplyBudget.ViewModels
{
    internal class ErrorProperty<T> : ViewModelBase
    {
        private T _value;
        public T Value
        {
            get => _value;
            set
            {
                if (SetProperty(ref _value, value))
                    ClearError();
            }
        }

        public bool IsError { get; private set; }

        public string Message { get; private set; }

        public void ClearError()
        {
            Message = null;
            IsError = false;
            OnPropertyChanged(nameof(IsError));
            OnPropertyChanged(nameof(Message));
        }

        public void SetError(string errorMessage)
        {
            Message = errorMessage;
            IsError = true;
            OnPropertyChanged(nameof(IsError));
            OnPropertyChanged(nameof(Message));
        }
    }
}