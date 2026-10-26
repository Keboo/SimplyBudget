using Microsoft.EntityFrameworkCore;
using Moq.AutoMock;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;
using SimplyBudgetWeb.Controllers;
using SimplyBudgetWeb.Data;

namespace SimplyBudgetWeb.Core.Tests.Controllers;

public class BudgetControllerTests
{
    [Test]
    public async Task Get_UsesSelectedMonthForTotalAccountAmount()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        await mocker.InDbScopeAsync(async context =>
        {
            var account = new Account
            {
                Name = "Checking",
                ValidatedDate = new DateTime(2026, 1, 1),
            };
            context.Accounts.Add(account);
            await context.SaveChangesAsync();

            var category = new ExpenseCategory
            {
                Name = "Checking Category",
                Description = "Tracks the checking account",
                AccountID = account.ID,
            };
            context.ExpenseCategories.Add(category);
            await context.SaveChangesAsync();

            context.ExpenseCategoryItems.AddRange(
                new ExpenseCategoryItem
                {
                    Date = new DateTime(2026, 1, 10),
                    Description = "January income",
                    Details =
                    [
                        new ExpenseCategoryItemDetail
                        {
                            Amount = 1000,
                            ExpenseCategoryId = category.ID,
                        },
                    ],
                },
                new ExpenseCategoryItem
                {
                    Date = new DateTime(2026, 3, 5),
                    Description = "March expense",
                    Details =
                    [
                        new ExpenseCategoryItemDetail
                        {
                            Amount = -300,
                            ExpenseCategoryId = category.ID,
                        },
                    ],
                });
            await context.SaveChangesAsync();
        });

        using var context = mocker.Get<BudgetWebContext>();
        context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;
        var controller = new BudgetController(context);

        var februaryBudget = await controller.Get(new DateTime(2026, 2, 1));
        var aprilBudget = await controller.Get(new DateTime(2026, 4, 1));

        await Assert.That(februaryBudget.TotalAccountAmount).IsEqualTo(1000);
        await Assert.That(aprilBudget.TotalAccountAmount).IsEqualTo(700);
        await Assert.That(februaryBudget.EstimatedMonthlyIncome).IsEqualTo(333);
        await Assert.That(aprilBudget.EstimatedMonthlyIncome).IsEqualTo(333);
        await Assert.That(februaryBudget.Categories.Single().Description).IsEqualTo("Tracks the checking account");
    }

    [Test]
    public async Task Get_EstimatedMonthlyIncome_UsesPriorThreeMonthsAndExcludesIgnoredBudgetItems()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        await mocker.InDbScopeAsync(async context =>
        {
            var primaryCategory = new ExpenseCategory
            {
                Name = "Primary",
                Description = "Primary category",
            };
            var transferCategory = new ExpenseCategory
            {
                Name = "Transfer",
                Description = "Transfer category",
            };

            context.ExpenseCategories.AddRange(primaryCategory, transferCategory);
            await context.SaveChangesAsync();

            context.ExpenseCategoryItems.AddRange(
                new ExpenseCategoryItem
                {
                    Date = new DateTime(2026, 1, 3),
                    Description = "January income",
                    Details =
                    [
                        new ExpenseCategoryItemDetail
                        {
                            Amount = 3000,
                            ExpenseCategoryId = primaryCategory.ID,
                        },
                    ],
                },
                new ExpenseCategoryItem
                {
                    Date = new DateTime(2026, 2, 8),
                    Description = "February income",
                    Details =
                    [
                        new ExpenseCategoryItemDetail
                        {
                            Amount = 6000,
                            ExpenseCategoryId = primaryCategory.ID,
                        },
                    ],
                },
                new ExpenseCategoryItem
                {
                    Date = new DateTime(2026, 3, 15),
                    Description = "March income",
                    Details =
                    [
                        new ExpenseCategoryItemDetail
                        {
                            Amount = 9000,
                            ExpenseCategoryId = primaryCategory.ID,
                        },
                    ],
                },
                new ExpenseCategoryItem
                {
                    Date = new DateTime(2026, 3, 18),
                    Description = "Ignored income",
                    Details =
                    [
                        new ExpenseCategoryItemDetail
                        {
                            Amount = 12000,
                            ExpenseCategoryId = primaryCategory.ID,
                            IgnoreBudget = true,
                        },
                    ],
                },
                new ExpenseCategoryItem
                {
                    Date = new DateTime(2026, 3, 20),
                    Description = "Transfer",
                    Details =
                    [
                        new ExpenseCategoryItemDetail
                        {
                            Amount = 5000,
                            ExpenseCategoryId = primaryCategory.ID,
                        },
                        new ExpenseCategoryItemDetail
                        {
                            Amount = -5000,
                            ExpenseCategoryId = transferCategory.ID,
                        },
                    ],
                },
                new ExpenseCategoryItem
                {
                    Date = new DateTime(2026, 4, 5),
                    Description = "Current month income",
                    Details =
                    [
                        new ExpenseCategoryItemDetail
                        {
                            Amount = 15000,
                            ExpenseCategoryId = primaryCategory.ID,
                        },
                    ],
                });
            await context.SaveChangesAsync();
        });

        using var context = mocker.Get<BudgetWebContext>();
        context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;
        var controller = new BudgetController(context);

        var aprilBudget = await controller.Get(new DateTime(2026, 4, 1));

        await Assert.That(aprilBudget.EstimatedMonthlyIncome).IsEqualTo(6000);
    }

    [Test]
    public async Task Get_CategoryAverages_DivideByRequestedMonthCount()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        await mocker.InDbScopeAsync(async context =>
        {
            var category = new ExpenseCategory
            {
                Name = "Taxes",
                Description = "Tax payments",
            };
            context.ExpenseCategories.Add(category);
            await context.SaveChangesAsync();

            context.ExpenseCategoryItems.AddRange(
                new ExpenseCategoryItem
                {
                    Date = new DateTime(2025, 10, 7),
                    Description = "October tax payment",
                    Details =
                    [
                        new ExpenseCategoryItemDetail
                        {
                            Amount = -3000,
                            ExpenseCategoryId = category.ID,
                        },
                    ],
                },
                new ExpenseCategoryItem
                {
                    Date = new DateTime(2025, 12, 10),
                    Description = "December tax payment",
                    Details =
                    [
                        new ExpenseCategoryItemDetail
                        {
                            Amount = -3000,
                            ExpenseCategoryId = category.ID,
                        },
                    ],
                });
            await context.SaveChangesAsync();
        });

        using var context = mocker.Get<BudgetWebContext>();
        context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;
        var controller = new BudgetController(context);

        var aprilBudget = await controller.Get(new DateTime(2026, 4, 1));
        var category = aprilBudget.Categories.Single();

        await Assert.That(category.ThreeMonthAverage).IsEqualTo(0d);
        await Assert.That(category.SixMonthAverage).IsEqualTo(500d);
        await Assert.That(category.TwelveMonthAverage).IsEqualTo(500d);
    }

    [Test]
    [Arguments(2026, 1)]
    [Arguments(2026, 10)]
    public async Task Get_CategoryAverages_MatchChartSpendingForSelectedMonth(int year, int month)
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();
        var selectedMonth = new DateTime(year, month, 1);

        int categoryId = await mocker.InDbScopeAsync(async context =>
        {
            var category = new ExpenseCategory { Name = "Car Insurance", BudgetedAmount = 14000 };
            var other = new ExpenseCategory { Name = "Savings" };
            context.ExpenseCategories.AddRange(category, other);
            await context.SaveChangesAsync();

            void AddExpense(DateTime date, int amount, bool ignored = false)
            {
                context.ExpenseCategoryItems.Add(new ExpenseCategoryItem
                {
                    Date = date,
                    Details = [new ExpenseCategoryItemDetail
                    {
                        ExpenseCategoryId = category.ID, Amount = amount, IgnoreBudget = ignored,
                    }],
                });
            }

            AddExpense(selectedMonth.AddMonths(-12), -90000); // Outside the chart.
            AddExpense(selectedMonth.AddMonths(-11), -12000);
            AddExpense(selectedMonth.AddMonths(-6), -6000);
            AddExpense(selectedMonth.AddMonths(-5), -5000);
            AddExpense(selectedMonth.AddMonths(-3), -3000);
            AddExpense(selectedMonth.AddMonths(-2), -2000);
            AddExpense(selectedMonth, -12692);
            AddExpense(selectedMonth.EndOfMonth(), -1);
            AddExpense(selectedMonth.AddMonths(1), -80000);
            AddExpense(selectedMonth, -70000, ignored: true);
            AddExpense(selectedMonth, 40000);
            context.ExpenseCategoryItems.Add(new ExpenseCategoryItem
            {
                Date = selectedMonth.AddMonths(-1),
                Details =
                [
                    new ExpenseCategoryItemDetail { ExpenseCategoryId = category.ID, Amount = -50000 },
                    new ExpenseCategoryItemDetail { ExpenseCategoryId = other.ID, Amount = 50000 },
                ],
            });
            await context.SaveChangesAsync();
            return category.ID;
        });

        using var context = mocker.Get<BudgetWebContext>();
        context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;
        var budget = await new BudgetController(context).Get(selectedMonth.AddDays(14));
        var chartResult = await new ExpenseCategoriesController(context)
            .GetMonthlyExpenses(categoryId, selectedMonth.AddDays(14));
        var chart = chartResult.Value!;
        var category = budget.Categories.Single(c => c.Id == categoryId);

        int[] expectedAmounts = [12000, 0, 0, 0, 0, 6000, 5000, 0, 3000, 2000, 0, 12693];
        await Assert.That(chart.Months.Length).IsEqualTo(12);
        for (int index = 0; index < expectedAmounts.Length; index++)
        {
            await Assert.That(chart.Months[index].Month)
                .IsEqualTo(selectedMonth.AddMonths(index - 11).ToString("yyyy-MM"));
            await Assert.That(chart.Months[index].Amount).IsEqualTo(expectedAmounts[index]);
        }
        await Assert.That(category.MonthlyExpenses).IsEqualTo(12693);
        await Assert.That(category.ThreeMonthAverage).IsEqualTo(14693d / 3);
        await Assert.That(category.SixMonthAverage).IsEqualTo(22693d / 6);
        await Assert.That(category.TwelveMonthAverage).IsEqualTo(40693d / 12);
        await Assert.That(category.ThreeMonthAverage).IsEqualTo(chart.Months.TakeLast(3).Average(p => p.Amount));
        await Assert.That(category.SixMonthAverage).IsEqualTo(chart.Months.TakeLast(6).Average(p => p.Amount));
        await Assert.That(category.TwelveMonthAverage).IsEqualTo(chart.Months.Average(p => p.Amount));
        var emptyCategory = budget.Categories.Single(c => c.Id != categoryId);
        await Assert.That(emptyCategory.ThreeMonthAverage).IsEqualTo(0d);
        await Assert.That(emptyCategory.SixMonthAverage).IsEqualTo(0d);
        await Assert.That(emptyCategory.TwelveMonthAverage).IsEqualTo(0d);
    }
}
