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

        public static async Task<Account?> GetDefaultAccountAsync(this BudgetContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return await context.Accounts.FirstOrDefaultAsync(x => x.IsDefault) ??
                   await context.Accounts.FirstOrDefaultAsync();
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

        public static async Task<Transaction> AddTransaction(this BudgetContext context, 
            ExpenseCategory expenseCategory,
            int amount, string description, DateTime? date = null)
        {
            var transaction = new Transaction { Description = description };

            var transactionDate = date ?? DateTime.Now;
            transaction.Date = transactionDate.Date;

            context.Transactions.Add(transaction);
            await context.SaveChangesAsync();

            var transactionItem = new TransactionItem
            {
                Description = description,
                Amount = amount,
                ExpenseCategoryID = expenseCategory.ID,
                TransactionID = transaction.ID
            };

            context.TransactionItems.Add(transactionItem);
            await context.SaveChangesAsync();

            return transaction;
        }
    }
}
