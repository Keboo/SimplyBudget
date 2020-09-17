using System;

namespace SimplyBudget.Commands
{
    [Obsolete("Use IRelayCommand")]
    public interface IRaiseCanExecute
    {
        void NotifyCanExecuteChanged();
    }
}