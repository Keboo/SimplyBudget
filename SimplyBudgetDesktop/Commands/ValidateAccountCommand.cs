using System;
using SimplyBudget.ViewModels.Data;
using SimplyBudgetShared.Data;

namespace SimplyBudget.Commands
{
    public class ValidateAccountCommand : MarkupCommandExtension<ValidateAccountCommand>
    {
        // ReSharper disable EmptyConstructor
        public ValidateAccountCommand() { }
        // ReSharper restore EmptyConstructor

        public override async void Execute(object parameter)
        {
            var accountVM = parameter as AccountViewModel;
            if (accountVM != null)
            {
                var account = await BudgetContext.Instance.Accounts.FindAsync(accountVM.AccountID);
                if (account != null)
                {
                    account.ValidatedDate = DateTime.Today;
                    await account.Save();
                }
            }
        }

        public override bool CanExecute(object parameter)
        {
            var accountVM = parameter as AccountViewModel;
            if (accountVM != null)
                return accountVM.LastValidatedDate.Date != DateTime.Today;
            return false;
        }
    }
}