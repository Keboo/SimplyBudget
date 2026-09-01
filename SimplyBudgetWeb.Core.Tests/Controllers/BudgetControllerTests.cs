using Microsoft.EntityFrameworkCore;
using Moq.AutoMock;
using SimplyBudgetShared.Data;
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
}
