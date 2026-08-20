using Moq.AutoMock;
using SimplyBudgetShared.Data;
using SimplyBudgetWeb.Controllers;
using SimplyBudgetWeb.Data;

namespace SimplyBudgetWeb.Core.Tests.Controllers;

public class AccountsControllerTests
{
    [Test]
    public async Task GetAll_UsesSelectedMonthForCurrentAmount()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        int accountId = await mocker.InDbScopeAsync(async context =>
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

            return account.ID;
        });

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new AccountsController(context);

        var februaryAccounts = await controller.GetAll(new DateTime(2026, 2, 1));
        var aprilAccounts = await controller.GetAll(new DateTime(2026, 4, 1));

        await Assert.That(februaryAccounts.Single(x => x.Id == accountId).CurrentAmount).IsEqualTo(1000);
        await Assert.That(aprilAccounts.Single(x => x.Id == accountId).CurrentAmount).IsEqualTo(700);
    }
}
