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
    }
}
