using System.Security.Claims;

using Microsoft.EntityFrameworkCore;
using SimplyBudgetWeb.Data;
using SimplyBudgetWeb.Services;

namespace SimplyBudgetWeb.Core.Tests.Services;

public class CurrentUserSyncServiceTests
{
    private static ClaimsPrincipal MakeUser(string objectId, string? name = null, string? preferredUsername = null)
    {
        var claims = new List<Claim> { new("oid", objectId) };
        if (name is not null) claims.Add(new Claim("name", name));
        if (preferredUsername is not null) claims.Add(new Claim("preferred_username", preferredUsername));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    [Test]
    public async Task SyncAsync_CreatesAssignee_ForNewUser()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        using var context = mocker.Get<BudgetWebContext>();
        var service = new CurrentUserSyncService(context);

        await service.SyncAsync(MakeUser("user-oid-1", name: "Jordan", preferredUsername: "jordan@example.com"));

        var assignee = await context.PendingExpenseAssignees.SingleAsync();
        await Assert.That(assignee.ObjectId).IsEqualTo("user-oid-1");
        await Assert.That(assignee.Name).IsEqualTo("Jordan");
        await Assert.That(assignee.Email).IsEqualTo("jordan@example.com");
    }

    [Test]
    public async Task SyncAsync_DoesNotDuplicate_ForReturningUser()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        using var context = mocker.Get<BudgetWebContext>();
        var service = new CurrentUserSyncService(context);

        await service.SyncAsync(MakeUser("user-oid-1", name: "Jordan"));
        await service.SyncAsync(MakeUser("user-oid-1", name: "Jordan"));

        await Assert.That(await context.PendingExpenseAssignees.CountAsync()).IsEqualTo(1);
    }

    [Test]
    public async Task SyncAsync_UpdatesName_WhenDisplayNameChanges()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        using var context = mocker.Get<BudgetWebContext>();
        var service = new CurrentUserSyncService(context);

        await service.SyncAsync(MakeUser("user-oid-1", name: "Jordan"));
        await service.SyncAsync(MakeUser("user-oid-1", name: "Jordan Smith"));

        var assignee = await context.PendingExpenseAssignees.SingleAsync();
        await Assert.That(assignee.Name).IsEqualTo("Jordan Smith");
    }

    [Test]
    public async Task SyncAsync_DoesNotOverrideCustomizedName()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        using var context = mocker.Get<BudgetWebContext>();
        var service = new CurrentUserSyncService(context);

        await service.SyncAsync(MakeUser("user-oid-1", name: "Jordan", preferredUsername: "jordan@example.com"));

        var assignee = await context.PendingExpenseAssignees.SingleAsync();
        assignee.Name = "Custom Jordan";
        assignee.IsNameCustomized = true;
        await context.SaveChangesAsync();

        await service.SyncAsync(MakeUser("user-oid-1", name: "Jordan Smith", preferredUsername: "jordan.smith@example.com"));

        var updatedAssignee = await context.PendingExpenseAssignees.SingleAsync();
        await Assert.That(updatedAssignee.Name).IsEqualTo("Custom Jordan");
        await Assert.That(updatedAssignee.Email).IsEqualTo("jordan.smith@example.com");
    }

    [Test]
    public async Task SyncAsync_IgnoresUser_WithoutObjectId()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        using var context = mocker.Get<BudgetWebContext>();
        var service = new CurrentUserSyncService(context);

        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        await service.SyncAsync(anonymous);

        await Assert.That(await context.PendingExpenseAssignees.CountAsync()).IsEqualTo(0);
    }
}
