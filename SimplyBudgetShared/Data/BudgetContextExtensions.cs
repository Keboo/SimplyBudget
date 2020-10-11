﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Toolkit.Mvvm.Messaging;
using SimplyBudgetShared.Utilities;
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

            var category = await context.ExpenseCategories.FindAsync(expenseCategory.ID);
            category.CurrentBalance += amount;

            await context.SaveChangesAsync();

            return incomeItem;
        }

        public static async Task<Income> AddIncome(this BudgetContext context,
            string description, DateTime date,
            params (int amount, int expenseCategory)[] items)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var income = new Income
            {
                Date = date,
                Description = description,
                TotalAmount = items.Sum(x => x.amount)
            };
            context.Add(income);
            await context.SaveChangesAsync();

            foreach((int amount, int expenseCategoryId) in items)
            {
                var incomeItem = new IncomeItem
                {
                    Amount = amount,
                    Description = description ?? income.Description,
                    ExpenseCategoryID = expenseCategoryId,
                    IncomeID = income.ID
                };
                context.Add(incomeItem);
                var category = await context.ExpenseCategories.FindAsync(expenseCategoryId);
                category.CurrentBalance += amount;
            }
            await context.SaveChangesAsync();

            return income;
        }

        public static async Task<Transaction> AddTransaction(this BudgetContext context,
            string description, DateTime date, params (int amount, int expenseCategory)[] items)
        {
            var transaction = new Transaction 
            { 
                Description = description,
                Date = date.Date
            };
            context.Transactions.Add(transaction);
            await context.SaveChangesAsync();
            
            foreach((int amount, int expenseCategoryId) in items)
            {
                var category = await context.ExpenseCategories.FindAsync(expenseCategoryId);
                category.CurrentBalance -= amount;
                
                var transactionItem = new TransactionItem
                {
                    Description = description,
                    Amount = amount,
                    ExpenseCategoryID = category.ID,
                    TransactionID = transaction.ID
                };
                context.TransactionItems.Add(transactionItem);
            }
            
            await context.SaveChangesAsync();

            return transaction;
        }

        public static async Task<Transaction> AddTransaction(this BudgetContext context,
            int expenseCategoryId,
            int amount, string description, DateTime? date = null)
        {
            return await AddTransaction(context, description, date ?? DateTime.Today, (amount, expenseCategoryId));
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
            var incomeIds = await context.Incomes.Where(x => x.Date >= start && x.Date <= end).Select(x => x.ID).ToListAsync();
            var allocatedAMount = (await context.IncomeItems.Where(x => x.ExpenseCategoryID == expenseCategory.ID && incomeIds.Contains(x.IncomeID))
                .ToListAsync())
                .Sum(x => x.Amount);
            return Math.Max(0, expenseCategory.BudgetedAmount - allocatedAMount);
        }

        public static async Task InitDatabase(this BudgetContext context, string storageFolder, string? dbFileName = null)
        {
            var builder = new DbContextOptionsBuilder<BudgetContext>()
                .UseSqlite($"Data Source={Path.Combine(storageFolder, dbFileName ?? "data.db")}");

            new BudgetContext(Messenger.Default, builder.Options);
        }
    }
}
