using Microsoft.EntityFrameworkCore;
using SimplyBudgetShared.Utilities;
using System;
using System.Collections.Generic;
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

            return account?.ExpenseCategories?.Sum(x => x.CurrentBalance) ?? 0;
        }

        public static async Task<ExpenseCategoryItem> AddTransfer(this BudgetContext context,
            string description, DateTime date, int amount,
            ExpenseCategory fromCategory, ExpenseCategory toCategory)
        {
            var item = new ExpenseCategoryItem
            {
                Date = date.Date,
                Description = description,
                Details = new List<ExpenseCategoryItemDetail>
                {
                    new ExpenseCategoryItemDetail
                    {
                        Amount = -amount,
                        ExpenseCategoryId = fromCategory.ID,
                    },
                    new ExpenseCategoryItemDetail
                    {
                        Amount = amount,
                        ExpenseCategoryId = toCategory.ID
                    }
                }
            };
            context.ExpenseCategoryItems.Add(item);

            await context.SaveChangesAsync();

            return item;
        }

        public static async Task AddIncome(this BudgetContext context,
            string description, DateTime date,
            params (int Amount, int ExpenseCategory)[] items)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var item = new ExpenseCategoryItem
            {
                Date = date.Date,
                Description = description,
                Details = items.Select(x =>
                {
                    return new ExpenseCategoryItemDetail
                    {
                        Amount = x.Amount,
                        ExpenseCategoryId = x.ExpenseCategory,
                    };
                }).ToList()
            };
            context.ExpenseCategoryItems.Add(item);
            await context.SaveChangesAsync();
        }

        public static async Task<ExpenseCategoryItem> AddTransaction(this BudgetContext context,
            string description, DateTime date, params (int amount, int expenseCategory)[] items)
        {
            var item = new ExpenseCategoryItem
            {
                Date = date.Date,
                Description = description,
                Details = items.Select(x =>
                {
                    return new ExpenseCategoryItemDetail
                    {
                        Amount = -x.amount,
                        ExpenseCategoryId = x.expenseCategory,
                    };
                }).ToList()
            };

            context.ExpenseCategoryItems.Add(item);
            await context.SaveChangesAsync();
            return item;
        }

        public static async Task<ExpenseCategoryItem> AddTransaction(this BudgetContext context,
            int expenseCategoryId,
            int amount, string description, DateTime? date = null)
        {
            return await AddTransaction(context, description, date ?? DateTime.Today, (amount, expenseCategoryId));
        }

        public static async Task<IList<ExpenseCategoryItem>> GetTransfers(this BudgetContext context, ExpenseCategory expenseCategory, DateTime? queryStart = null, DateTime? queryEnd = null)
        {
            IQueryable<ExpenseCategoryItem> transferQuery = context.ExpenseCategoryItems.Include(x => x.Details)
                .Where(x => x.Details!.Count() == 2 &&
                       x.Details!.Sum(x => x.Amount) == 0 &&
                       x.Details!.Any(x => x.ExpenseCategoryId == expenseCategory.ID));
            if (queryStart != null && queryEnd != null)
            {
                transferQuery = transferQuery.Where(x => x.Date >= queryStart && x.Date <= queryEnd);
            }
            return await transferQuery.ToListAsync();
        }

        public static async Task<IList<ExpenseCategoryItemDetail>> GetCategoryItemDetails(this BudgetContext context, ExpenseCategory expenseCategory, DateTime? queryStart = null, DateTime? queryEnd = null)
        {
            IQueryable<ExpenseCategoryItemDetail> query = context.ExpenseCategoryItemDetails
                .Include(x => x.ExpenseCategoryItem)
                .ThenInclude(x => x!.Details)
                .Where(x => x.ExpenseCategoryId == expenseCategory.ID);

            if (queryStart != null && queryEnd != null)
            {
                query = query
                    .Where(x => x.ExpenseCategoryItem!.Date >= queryStart && x.ExpenseCategoryItem.Date <= queryEnd);
            }
            return await query.ToListAsync();
        }

        public static async Task<int> GetRemainingBudgetAmount(this BudgetContext context, ExpenseCategory expenseCategory, DateTime month)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (expenseCategory is null)
            {
                throw new ArgumentNullException(nameof(expenseCategory));
            }

            if (expenseCategory.UsePercentage) return 0;

            var start = month.StartOfMonth();
            var end = month.EndOfMonth();

            var allocatedAmount =
                await context.ExpenseCategoryItemDetails
                    .Include(x => x.ExpenseCategoryItem)
                    .Where(x => x.ExpenseCategoryId == expenseCategory.ID)
                    .Where(x => x.Amount > 0)
                    .Where(x => x.ExpenseCategoryItem!.Date >= start && x.ExpenseCategoryItem.Date <= end)
                    .SumAsync(x => x.Amount);
            int remaining = Math.Max(0, expenseCategory.BudgetedAmount - allocatedAmount);
            if (expenseCategory.Cap is int cap)
            {
                return Math.Min(remaining, Math.Max(0, cap - expenseCategory.CurrentBalance));
            }
            return remaining;
        }
    }
}
