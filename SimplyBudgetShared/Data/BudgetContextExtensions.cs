using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SimplyBudgetShared.Data
{
    public static class BudgetContextExtensions
    {
        public static async Task<Account> SetAsDefaultAsync(this BudgetContext context, Account account)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (account is null)
            {
                throw new ArgumentNullException(nameof(account));
            }

            if (await context.Accounts.SingleOrDefaultAsync(x => x.IsDefault) is { } previousDefault)
            {
                previousDefault.IsDefault = false;
            }
            if (await context.Accounts.SingleOrDefaultAsync(x => x.ID == account.ID) is { } newDefault)
            {
                newDefault.IsDefault = true;
                return newDefault;
            }
            account.IsDefault = true;
            context.Accounts.Add(account);
            return account;
        }


        public static async Task<int> GetCurrentAmount(this BudgetContext context, int accountId)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            Account account = await context.Accounts
                .Include(x => x.ExpenseCategories)
                .FirstOrDefaultAsync(x => x.ID == accountId);

            return account?.ExpenseCategories.Sum(x => x.CurrentBalance) ?? 0;
        }
    }
}
