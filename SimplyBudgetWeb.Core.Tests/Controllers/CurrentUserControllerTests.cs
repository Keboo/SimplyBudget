using System.Security.Claims;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplyBudgetWeb.Controllers;
using SimplyBudgetWeb.Data;

namespace SimplyBudgetWeb.Core.Tests.Controllers;

public class CurrentUserControllerTests
{
    private static ClaimsPrincipal MakeUser(string objectId)
    {
        var identity = new ClaimsIdentity([new Claim("oid", objectId)], "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    private static CurrentUserController CreateController(BudgetWebContext context, ClaimsPrincipal user)
    {
        var controller = new CurrentUserController(context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = user,
                },
            },
        };

        return controller;
    }

    [Test]
    public async Task Get_ReturnsCurrentUser()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        await mocker.InDbScopeAsync(async context =>
        {
            context.PendingExpenseAssignees.Add(new PendingExpenseAssignee
            {
                Name = "Jordan",
                ObjectId = "user-oid-1",
                Email = "jordan@example.com",
            });
            await context.SaveChangesAsync();
        });

        using var context = mocker.Get<BudgetWebContext>();
        var controller = CreateController(context, MakeUser("user-oid-1"));

        var result = await controller.Get();

        var dto = result.Value ?? ((OkObjectResult)result.Result!).Value as CurrentUserDto;
        await Assert.That(dto).IsNotNull();
        await Assert.That(dto!.DisplayName).IsEqualTo("Jordan");
        await Assert.That(dto.Email).IsEqualTo("jordan@example.com");
    }

    [Test]
    public async Task UpdateDisplayName_PersistsNameAndMarksAsCustomized()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        await mocker.InDbScopeAsync(async context =>
        {
            context.PendingExpenseAssignees.Add(new PendingExpenseAssignee
            {
                Name = "Jordan",
                ObjectId = "user-oid-1",
                IsNameCustomized = false,
            });
            await context.SaveChangesAsync();
        });

        using (var context = mocker.Get<BudgetWebContext>())
        {
            var controller = CreateController(context, MakeUser("user-oid-1"));
            var result = await controller.UpdateDisplayName(new CurrentUserDisplayNameRequest("  Jordan S.  "));

            var dto = result.Value ?? ((OkObjectResult)result.Result!).Value as CurrentUserDto;
            await Assert.That(dto).IsNotNull();
            await Assert.That(dto!.DisplayName).IsEqualTo("Jordan S.");
        }

        await mocker.InDbScopeAsync(async context =>
        {
            var assignee = await context.PendingExpenseAssignees.SingleAsync(x => x.ObjectId == "user-oid-1");
            await Assert.That(assignee.Name).IsEqualTo("Jordan S.");
            await Assert.That(assignee.IsNameCustomized).IsTrue();
        });
    }

    [Test]
    public async Task UpdateDisplayName_WithBlankName_ReturnsBadRequest()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        await mocker.InDbScopeAsync(async context =>
        {
            context.PendingExpenseAssignees.Add(new PendingExpenseAssignee
            {
                Name = "Jordan",
                ObjectId = "user-oid-1",
            });
            await context.SaveChangesAsync();
        });

        using var context = mocker.Get<BudgetWebContext>();
        var controller = CreateController(context, MakeUser("user-oid-1"));

        var result = await controller.UpdateDisplayName(new CurrentUserDisplayNameRequest("   "));

        await Assert.That(result.Result).IsTypeOf<BadRequestObjectResult>();
    }
}
