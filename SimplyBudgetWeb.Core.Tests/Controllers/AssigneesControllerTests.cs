using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplyBudgetWeb.Controllers;
using SimplyBudgetWeb.Data;

namespace SimplyBudgetWeb.Core.Tests.Controllers;

public class AssigneesControllerTests
{
    [Test]
    public async Task GetAll_ReturnsAssigneesOrderedByName()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        await mocker.InDbScopeAsync(async context =>
        {
            context.PendingExpenseAssignees.AddRange(
                new PendingExpenseAssignee { Name = "Zoe", ObjectId = "zoe-oid" },
                new PendingExpenseAssignee { Name = "Alice", ObjectId = "alice-oid" });
            await context.SaveChangesAsync();
        });

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new AssigneesController(context);

        var result = await controller.GetAll();

        await Assert.That(result.Length).IsEqualTo(2);
        await Assert.That(result[0].Name).IsEqualTo("Alice");
        await Assert.That(result[1].Name).IsEqualTo("Zoe");
    }

    [Test]
    public async Task Delete_RemovesAssignee()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        int assigneeId = await mocker.InDbScopeAsync(async context =>
        {
            var assignee = new PendingExpenseAssignee { Name = "Jordan", ObjectId = "jordan-oid" };
            context.PendingExpenseAssignees.Add(assignee);
            await context.SaveChangesAsync();
            return assignee.ID;
        });

        using (var context = mocker.Get<BudgetWebContext>())
        {
            var controller = new AssigneesController(context);
            var result = await controller.Delete(assigneeId);
            await Assert.That(result).IsTypeOf<NoContentResult>();
        }

        await mocker.InDbScopeAsync(async context =>
        {
            await Assert.That(await context.PendingExpenseAssignees.CountAsync()).IsEqualTo(0);
        });
    }

    [Test]
    public async Task Delete_WithUnknownId_ReturnsNotFound()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new AssigneesController(context);

        var result = await controller.Delete(999);

        await Assert.That(result).IsTypeOf<NotFoundResult>();
    }
}
