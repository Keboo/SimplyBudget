using SimplyBudgetShared.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimplyBudgetShared.Data;

public static class SampleBudget
{
    //fixed seed just so things are consistent
    private static Random Random = new(87654321);

    public static Account CheckingAccount { get; } = new Account { Name = "Checking", IsDefault = true };
    public static Account SavingsAccount { get; } = new Account { Name = "Savings" };

    public static async Task GenerateBudget(BudgetContext context)
    {
        context.Accounts.Add(CheckingAccount);
        context.Accounts.Add(SavingsAccount);

        await AddExpenseCategories(context);
    }

    private static async Task AddExpenseCategories(BudgetContext context)
    {
        await AddCategory("Toiletries / Cosmetics",
            100_00,
            0,
            CheckingAccount,
            "Personal",
            1..2,
            2..5);

        await AddCategory(
            "Houshold Products",
            120_00,
            0,
            CheckingAccount,
            "Personal",
            1..2,
            2..5);

        await AddCategory(
            "Clothing",
            120_00,
            0,
            CheckingAccount,
            "Personal",
            1..2,
            2..5);

        await AddCategory(
            "Misc Personal",
            40_00,
            0,
            CheckingAccount,
            "Personal",
            0..2,
            0..2);

        await AddCategory(
            "Emergency Fund",
            0,
            10,
            SavingsAccount,
            "Savings and Investments",
            1..2,
            0..1);

        await AddCategory(
            "College Fund",
            80_00,
            0,
            SavingsAccount,
            "Savings and Investments",
            1..2,
            0..0);

        await AddCategory(
            "Savings",
            0,
            0,
            SavingsAccount,
            "Savings and Investments",
            0..1,
            0..0);

        await AddCategory(
            "Charitable Donations",
            0,
            10,
            CheckingAccount,
            "Savings and Investments",
            1..1,
            1..1);

        await AddCategory(
            "Utilities",
            350_00,
            0,
            CheckingAccount,
            "Bills",
            1..2,
            1..1);
        await AddCategory(
            "Internet",
            100_00,
            0,
            CheckingAccount,
            "Bills",
            1..2,
            1..1);
        await AddCategory(
            "Cell Phone",
            110_00,
            0,
            CheckingAccount,
            "Bills",
            1..2,
            1..1);

        async Task AddCategory(string name, int amount, int percentage, Account account, string category,
            Range incomeRange, Range expenseRange)
        {
            var ec = new ExpenseCategory
            {
                Name = name,
                BudgetedAmount = amount,
                BudgetedPercentage = percentage,
                Account = account,
                CategoryName = category
            };
            context.ExpenseCategories.Add(ec);
            await context.SaveChangesAsync();

            foreach (var item in GenerateItems(ec, incomeRange, expenseRange))
            {
                context.ExpenseCategoryItems.Add(item);
            }
            await context.SaveChangesAsync();
        }
    }

    private static IEnumerable<ExpenseCategoryItem> GenerateItems(
        ExpenseCategory category, Range incomeRange, Range expenseRange)
    {

        foreach (var m in Enumerable.Range(0, 13))
        {
            int numExpense = Random.Next(expenseRange.Start.Value, expenseRange.End.Value + 1);
            DateTime month = DateTime.Today.AddMonths(-m);
            for (int i = 0; i < numExpense; i++)
            {
                int amount;
                if (category.UsePercentage)
                {
                    amount = Random.Next(0_10, 150_00);
                }
                else
                {
                    int target = category.BudgetedAmount / numExpense;
                    amount = Random.Next((int)(target * 0.8), (int)(target * 1.2));
                }
                yield return new ExpenseCategoryItem
                {
                    Date = RandomDateInMonth(month),
                    Description = $"Expense {Guid.NewGuid().ToString()[0..5]}",
                    Details = new()
                    {
                        new ExpenseCategoryItemDetail
                        {
                            Amount = -amount,
                            ExpenseCategory = category
                        }
                    }
                };
            }

            int numIncome = Random.Next(incomeRange.Start.Value, incomeRange.End.Value + 1);
            for (int i = 0; i < numIncome; i++)
            {
                int amount;
                if (category.UsePercentage)
                {
                    amount = Random.Next(0_10, 150_00);
                }
                else
                {
                    int target = category.BudgetedAmount / numIncome;
                    amount = Random.Next((int)(target * 0.8), (int)(target * 1.2));
                }
                yield return new ExpenseCategoryItem
                {
                    Date = RandomDateInMonth(month),
                    Description = $"Income {Guid.NewGuid().ToString()[0..5]}",
                    Details = new()
                    {
                        new ExpenseCategoryItemDetail
                        {
                            Amount = amount,
                            ExpenseCategory = category
                        }
                    }
                };
            }
        }

        DateTime RandomDateInMonth(DateTime month)
        {
            var startOfMonth = month.EndOfMonth();
            var day = Random.Next(startOfMonth.Day + 1);
            return startOfMonth.AddDays(day);
        }
    }

    //public static async Task CreateDefaultBudget()
    //{
    //    //Create accounts
    //    var savingsAccount = new Account { Name = "Savings", ValidatedDate = DateTime.Today };
    //    new Account { Name = "Checking", ValidatedDate = DateTime.Today, IsDefault = true };
    //
    //    //Giving
    //    new ExpenseCategory { CategoryName = "Giving", Name = "Charitable Contributions", BudgetedPercentage = 10 };
    //
    //    //Savings
    //    new ExpenseCategory { CategoryName = "Saving", Name = "Emergency Fund", BudgetedAmount = 15000, AccountID = savingsAccount.ID };
    //    new ExpenseCategory { CategoryName = "Saving", Name = "Retirement", BudgetedAmount = 10000, AccountID = savingsAccount.ID };
    //    new ExpenseCategory { CategoryName = "Saving", Name = "Investments", BudgetedAmount = 10000, AccountID = savingsAccount.ID };
    //
    //    //Housing
    //    new ExpenseCategory { CategoryName = "Housing", Name = "Mortgage", BudgetedAmount = 80000 };
    //    new ExpenseCategory { CategoryName = "Housing", Name = "Home Repairs", BudgetedAmount = 4000 };
    //    new ExpenseCategory { CategoryName = "Housing", Name = "Replace Furniture", BudgetedAmount = 3000 };
    //    new ExpenseCategory { CategoryName = "Housing", Name = "Home Decor", BudgetedAmount = 2000 };
    //
    //    //Utilities
    //    new ExpenseCategory { CategoryName = "Utilities", Name = "Utilities 1", BudgetedAmount = 20000 };
    //    new ExpenseCategory { CategoryName = "Utilities", Name = "Utilities 2", BudgetedAmount = 15000 };
    //    new ExpenseCategory { CategoryName = "Utilities", Name = "Utilities 3", BudgetedAmount = 15000 };
    //    new ExpenseCategory { CategoryName = "Utilities", Name = "Cell Phone", BudgetedAmount = 8000 };
    //
    //    //Food
    //    new ExpenseCategory { CategoryName = "Food", Name = "Grocery", BudgetedAmount = 20000 };
    //    new ExpenseCategory { CategoryName = "Food", Name = "Restaurants", BudgetedAmount = 5000 };
    //
    //    //Transportation
    //    new ExpenseCategory { CategoryName = "Transportation", Name = "Gas", BudgetedAmount = 20000 };
    //    new ExpenseCategory { CategoryName = "Transportation", Name = "Car Maintenance", BudgetedAmount = 4000 };
    //    new ExpenseCategory { CategoryName = "Transportation", Name = "Car Insurance", BudgetedAmount = 15000 };
    //    new ExpenseCategory { CategoryName = "Transportation", Name = "License and Taxes", BudgetedAmount = 2500 };
    //    new ExpenseCategory { CategoryName = "Transportation", Name = "Car Replacement", BudgetedAmount = 5000 };
    //
    //    //Personal
    //    new ExpenseCategory { CategoryName = "Personal", Name = "Life Insurance", BudgetedAmount = 2500 };
    //    new ExpenseCategory { CategoryName = "Personal", Name = "Pets", BudgetedAmount = 10000 };
    //    new ExpenseCategory { CategoryName = "Personal", Name = "Clothing", BudgetedAmount = 5000 };
    //    new ExpenseCategory { CategoryName = "Personal", Name = "Household Items", BudgetedAmount = 4000 };
    //    new ExpenseCategory { CategoryName = "Personal", Name = "Toiletries", BudgetedAmount = 4000 };
    //    new ExpenseCategory { CategoryName = "Personal", Name = "Cosmetics", BudgetedAmount = 4000 };
    //    new ExpenseCategory { CategoryName = "Personal", Name = "Organization Dues", BudgetedAmount = 2000 };
    //    new ExpenseCategory { CategoryName = "Personal", Name = "Gifts (including Christmas)", BudgetedAmount = 10000 };
    //    new ExpenseCategory { CategoryName = "Personal", Name = "Blow Money (His)", BudgetedAmount = 5000 };
    //    new ExpenseCategory { CategoryName = "Personal", Name = "Blow Money (Hers)", BudgetedAmount = 5000 };
    //
    //    //Recreation
    //    new ExpenseCategory { CategoryName = "Recreation", Name = "Entertainment", BudgetedAmount = 5000 };
    //    new ExpenseCategory { CategoryName = "Recreation", Name = "Dates", BudgetedAmount = 5000 };
    //    new ExpenseCategory { CategoryName = "Recreation", Name = "Vacation", BudgetedAmount = 5000 };
    //
    //    //Debts
    //    new ExpenseCategory { CategoryName = "Debt", Name = "Credit Card 1", BudgetedAmount = 0 };
    //    new ExpenseCategory { CategoryName = "Debt", Name = "Credit Card 2", BudgetedAmount = 0 };
    //    new ExpenseCategory { CategoryName = "Debt", Name = "School Loans", BudgetedAmount = 0 };
    //}
}