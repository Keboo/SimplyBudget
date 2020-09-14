using JetBrains.Annotations;
using System;

namespace SimplyBudget.Utilities
{
    public class DelegateCommand : DelegateCommandBase
    {
        private readonly Action _executeAction;
        private readonly Func<bool> _canExecuteAction;
 
        public DelegateCommand([NotNull] Action executeAction, Func<bool> canExecuteAction = null)
        {
            if (executeAction == null) throw new ArgumentNullException("executeAction");
            _executeAction = executeAction;
            _canExecuteAction = canExecuteAction;
        }

        public override bool CanExecute(object parameter)
        {
            return _canExecuteAction == null ||
                   _canExecuteAction();
        }

        public override void Execute(object parameter)
        {
            _executeAction();
        }
    }

    public class DelegateCommand<T> : DelegateCommandBase
    {
        private readonly Action<T> _executeAction;
        private readonly Func<T, bool> _canExecuteAction;

        public DelegateCommand([NotNull] Action<T> executeAction, Func<T, bool> canExecuteAction = null)
        {
            if (executeAction == null) throw new ArgumentNullException("executeAction");
            _executeAction = executeAction;
            _canExecuteAction = canExecuteAction;
        }

        public override bool CanExecute(object parameter)
        {
            return _canExecuteAction == null ||
                   _canExecuteAction(ConvertValue(parameter));
        }

        public override void Execute(object parameter)
        {
            _executeAction(ConvertValue(parameter));
        }

        private T ConvertValue(object parameter)
        {
            return (T) Convert.ChangeType(parameter, typeof (T));
        }
    }
}