using System;
using System.Threading.Tasks;

namespace SimplyBudgetShared.Data
{
    public static class DefaultBudget
    {
        public static async Task CreateDefaultBudget()
        {
            //Create accounts
            var savingsAccount = await new Account { Name = "Savings", ValidatedDate = DateTime.Today }.Save();
            await new Account { Name = "Checking", ValidatedDate = DateTime.Today, IsDefault = true }.Save();

            //Giving
            await new ExpenseCategory { CategoryName = "Giving", Name = "Charitable Contributions", BudgetedPercentage = 10 }.Save();

            //Savings
            await new ExpenseCategory { CategoryName = "Saving", Name = "Emergency Fund", BudgetedAmount = 15000, AccountID = savingsAccount.ID }.Save();
            await new ExpenseCategory { CategoryName = "Saving", Name = "Retirement", BudgetedAmount = 10000, AccountID = savingsAccount.ID }.Save();
            await new ExpenseCategory { CategoryName = "Saving", Name = "Investments", BudgetedAmount = 10000, AccountID = savingsAccount.ID }.Save();

            //Housing
            await new ExpenseCategory { CategoryName = "Housing", Name = "Mortgage", BudgetedAmount = 80000 }.Save();
            await new ExpenseCategory { CategoryName = "Housing", Name = "Home Repairs", BudgetedAmount = 4000 }.Save();
            await new ExpenseCategory { CategoryName = "Housing", Name = "Replace Furniture", BudgetedAmount = 3000 }.Save();
            await new ExpenseCategory { CategoryName = "Housing", Name = "Home Decor", BudgetedAmount = 2000 }.Save();

            //Utilities
            await new ExpenseCategory { CategoryName = "Utilities", Name = "Utilities 1", BudgetedAmount = 20000 }.Save();
            await new ExpenseCategory { CategoryName = "Utilities", Name = "Utilities 2", BudgetedAmount = 15000 }.Save();
            await new ExpenseCategory { CategoryName = "Utilities", Name = "Utilities 3", BudgetedAmount = 15000 }.Save();
            await new ExpenseCategory { CategoryName = "Utilities", Name = "Cell Phone", BudgetedAmount = 8000 }.Save();

            //Food
            await new ExpenseCategory { CategoryName = "Food", Name = "Grocery", BudgetedAmount = 20000 }.Save();
            await new ExpenseCategory { CategoryName = "Food", Name = "Restaurants", BudgetedAmount = 5000 }.Save();

            //Transportation
            await new ExpenseCategory { CategoryName = "Transportation", Name = "Gas", BudgetedAmount = 20000 }.Save();
            await new ExpenseCategory { CategoryName = "Transportation", Name = "Car Maintenance", BudgetedAmount = 4000 }.Save();
            await new ExpenseCategory { CategoryName = "Transportation", Name = "Car Insurance", BudgetedAmount = 15000 }.Save();
            await new ExpenseCategory { CategoryName = "Transportation", Name = "License and Taxes", BudgetedAmount = 2500 }.Save();
            await new ExpenseCategory { CategoryName = "Transportation", Name = "Car Replacement", BudgetedAmount = 5000 }.Save();

            //Personal
            await new ExpenseCategory { CategoryName = "Personal", Name = "Life Insurance", BudgetedAmount = 2500 }.Save();
            await new ExpenseCategory { CategoryName = "Personal", Name = "Pets", BudgetedAmount = 10000 }.Save();
            await new ExpenseCategory { CategoryName = "Personal", Name = "Clothing", BudgetedAmount = 5000 }.Save();
            await new ExpenseCategory { CategoryName = "Personal", Name = "Household Items", BudgetedAmount = 4000 }.Save();
            await new ExpenseCategory { CategoryName = "Personal", Name = "Toiletries", BudgetedAmount = 4000 }.Save();
            await new ExpenseCategory { CategoryName = "Personal", Name = "Cosmetics", BudgetedAmount = 4000 }.Save();
            await new ExpenseCategory { CategoryName = "Personal", Name = "Organization Dues", BudgetedAmount = 2000 }.Save();
            await new ExpenseCategory { CategoryName = "Personal", Name = "Gifts (including Christmas)", BudgetedAmount = 10000 }.Save();
            await new ExpenseCategory { CategoryName = "Personal", Name = "Blow Money (His)", BudgetedAmount = 5000 }.Save();
            await new ExpenseCategory { CategoryName = "Personal", Name = "Blow Money (Hers)", BudgetedAmount = 5000 }.Save();

            //Recreation
            await new ExpenseCategory { CategoryName = "Recreation", Name = "Entertainment", BudgetedAmount = 5000 }.Save();
            await new ExpenseCategory { CategoryName = "Recreation", Name = "Dates", BudgetedAmount = 5000 }.Save();
            await new ExpenseCategory { CategoryName = "Recreation", Name = "Vacation", BudgetedAmount = 5000 }.Save();

            //Debts
            await new ExpenseCategory { CategoryName = "Debt", Name = "Credit Card 1", BudgetedAmount = 0 }.Save();
            await new ExpenseCategory { CategoryName = "Debt", Name = "Credit Card 2", BudgetedAmount = 0 }.Save();
            await new ExpenseCategory { CategoryName = "Debt", Name = "School Loans", BudgetedAmount = 0 }.Save();
        }
    }
}