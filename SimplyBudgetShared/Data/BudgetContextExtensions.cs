using Microsoft.EntityFrameworkCore;
using Microsoft.Toolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.IO;
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

        public static async Task<IncomeItem> AddIncomeItem(this BudgetContext context,
            ExpenseCategory expenseCategory, Income income,
            int amount, string? description = null)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (expenseCategory is null)
            {
                throw new ArgumentNullException(nameof(expenseCategory));
            }

            if (income is null)
            {
                throw new ArgumentNullException(nameof(income));
            }

            var incomeItem = new IncomeItem
            {
                Amount = amount,
                Description = description ?? income.Description,
                ExpenseCategoryID = expenseCategory.ID,
                IncomeID = income.ID
            };
            context.IncomeItems.Add(incomeItem);
            await context.SaveChangesAsync();

            return incomeItem;
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

        public static async Task<IList<Transfer>> GetTransfers(this BudgetContext context, ExpenseCategory expenseCategory, DateTime? queryStart = null, DateTime? queryEnd = null)
        {
            IQueryable<Transfer> transferQuery = context.Transfers.Where(x => x.FromExpenseCategoryID == expenseCategory.ID || x.ToExpenseCategoryID == expenseCategory.ID);

            if (queryStart != null && queryEnd != null)
            {
                transferQuery = transferQuery.Where(x => x.Date >= queryStart && x.Date <= queryEnd);
            }
            return await transferQuery.ToListAsync();
        }

        public static async Task<IList<TransactionItem>> GetTransactionItems(this BudgetContext context, ExpenseCategory expenseCategory, DateTime? queryStart = null, DateTime? queryEnd = null)
        {
            IQueryable<TransactionItem> transactionItemsQuery = context.TransactionItems.Where(x => x.ExpenseCategoryID == expenseCategory.ID);

            if (queryStart != null && queryEnd != null)
            {
                var transactions = await context.Transactions.Where(x => x.Date >= queryStart && x.Date <= queryEnd)
                    .Select(x => x.ID)
                    .ToListAsync();
                transactionItemsQuery = transactionItemsQuery.Where(x => transactions.Contains(x.TransactionID));
            }
            return await transactionItemsQuery.ToListAsync();
        }

        public static async Task<IList<IncomeItem>> GetIncomeItems(this BudgetContext context, ExpenseCategory expenseCategory, DateTime? queryStart = null, DateTime? queryEnd = null)
        {
            IQueryable<IncomeItem> incomeItemsQuery = context.IncomeItems.Where(x => x.ExpenseCategoryID == expenseCategory.ID);

            if (queryStart != null && queryEnd != null)
            {
                var incomes = await context.Incomes.Where(x => x.Date >= queryStart && x.Date <= queryEnd)
                    .Select(x => x.ID)
                    .ToListAsync();
                incomeItemsQuery = incomeItemsQuery.Where(x => incomes.Contains(x.IncomeID));
            }
            return await incomeItemsQuery.ToListAsync();
        }

        public static async Task InitDatabase(this BudgetContext context, string storageFolder, string? dbFileName = null)
        {
            var builder = new DbContextOptionsBuilder<BudgetContext>()
                .UseSqlite($"Data Source={Path.Combine(storageFolder, dbFileName ?? "data.db")}");

            new BudgetContext(Messenger.Default, builder.Options);
        }
    }
}
