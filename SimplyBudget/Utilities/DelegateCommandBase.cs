using System;
using System.Windows.Input;
using SimplyBudgetShared.Utilities;

namespace SimplyBudget.Utilities
{
    public abstract class DelegateCommandBase : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public abstract bool CanExecute(object parameter);

        public abstract void Execute(object parameter);

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged.Raise(this);
        }
    }
}