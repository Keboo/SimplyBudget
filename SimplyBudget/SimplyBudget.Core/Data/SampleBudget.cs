using SimplyBudget.Core.Utilities;

namespace SimplyBudget.Core.Data;

public static class SampleBudget
{
    //fixed seed just so things are consistent
    private static Random Random { get; } = new(87654321);

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
            "Household Products",
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

        static DateTime RandomDateInMonth(DateTime month)
        {
            var startOfMonth = month.EndOfMonth();
            var day = Random.Next(startOfMonth.Day + 1);
            return startOfMonth.AddDays(day);
        }
    }
}