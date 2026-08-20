using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplyBudgetShared.Data;
using SimplyBudgetWeb.Controllers;
using SimplyBudgetWeb.Data;

namespace SimplyBudgetWeb.Core.Tests.Controllers;

public class PendingExpensesControllerTests
{
    [Test]
    public async Task GetAll_ReturnsItemsOrderedByDateAscending_OldestFirst()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        await mocker.InDbScopeAsync(async context =>
        {
            context.PendingExpenses.AddRange(
                new PendingExpense { Date = new DateTime(2026, 3, 1), Description = "Newest", Amount = 100 },
                new PendingExpense { Date = new DateTime(2026, 1, 1), Description = "Oldest", Amount = 200 },
                new PendingExpense { Date = new DateTime(2026, 2, 1), Description = "Middle", Amount = 300 });
            await context.SaveChangesAsync();
        });

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new PendingExpensesController(context);

        var result = await controller.GetAll(search: null, assigneeId: null);

        await Assert.That(result.Length).IsEqualTo(3);
        await Assert.That(result[0].Description).IsEqualTo("Oldest");
        await Assert.That(result[1].Description).IsEqualTo("Middle");
        await Assert.That(result[2].Description).IsEqualTo("Newest");
    }

    [Test]
    public async Task GetAll_IsNotLimitedToASingleMonth()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        await mocker.InDbScopeAsync(async context =>
        {
            context.PendingExpenses.AddRange(
                new PendingExpense { Date = new DateTime(2024, 1, 1), Description = "Very old", Amount = 100 },
                new PendingExpense { Date = new DateTime(2026, 6, 1), Description = "Recent", Amount = 200 });
            await context.SaveChangesAsync();
        });

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new PendingExpensesController(context);

        var result = await controller.GetAll(search: null, assigneeId: null);

        await Assert.That(result.Length).IsEqualTo(2);
    }

    [Test]
    public async Task GetAll_WithMonthFilter_OnlyReturnsItemsFromThatMonth()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        await mocker.InDbScopeAsync(async context =>
        {
            context.PendingExpenses.AddRange(
                new PendingExpense { Date = new DateTime(2026, 6, 1), Description = "June 1", Amount = 100 },
                new PendingExpense { Date = new DateTime(2026, 6, 15), Description = "June 15", Amount = 200 },
                new PendingExpense { Date = new DateTime(2026, 7, 1), Description = "July 1", Amount = 300 });
            await context.SaveChangesAsync();
        });

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new PendingExpensesController(context);

        var result = await controller.GetAll(month: new DateTime(2026, 6, 10), search: null, assigneeId: null);

        await Assert.That(result.Length).IsEqualTo(2);
        await Assert.That(result[0].Description).IsEqualTo("June 1");
        await Assert.That(result[1].Description).IsEqualTo("June 15");
    }

    [Test]
    public async Task GetOldestMonth_ReturnsOldestPendingExpenseMonthStart()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        await mocker.InDbScopeAsync(async context =>
        {
            context.PendingExpenses.AddRange(
                new PendingExpense { Date = new DateTime(2026, 6, 15), Description = "Recent", Amount = 100 },
                new PendingExpense { Date = new DateTime(2024, 1, 31), Description = "Oldest", Amount = 200 });
            await context.SaveChangesAsync();
        });

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new PendingExpensesController(context);

        var result = await controller.GetOldestMonth();

        await Assert.That(result.Month).IsEqualTo(new DateTime(2024, 1, 1));
    }

    [Test]
    public async Task GetOldestMonth_WithNoPendingExpenses_ReturnsNull()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new PendingExpensesController(context);

        var result = await controller.GetOldestMonth();

        await Assert.That(result.Month).IsNull();
    }

    [Test]
    public async Task GetAll_FiltersBySearch()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        await mocker.InDbScopeAsync(async context =>
        {
            context.PendingExpenses.AddRange(
                new PendingExpense { Date = new DateTime(2026, 1, 1), Description = "Costco groceries", Amount = 100 },
                new PendingExpense { Date = new DateTime(2026, 1, 2), Description = "Gas station", Amount = 200 });
            await context.SaveChangesAsync();
        });

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new PendingExpensesController(context);

        var result = await controller.GetAll(search: "Costco", assigneeId: null);

        await Assert.That(result.Length).IsEqualTo(1);
        await Assert.That(result[0].Description).IsEqualTo("Costco groceries");
    }

    [Test]
    public async Task GetAll_FiltersByAssignee()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        int assigneeId = await mocker.InDbScopeAsync(async context =>
        {
            var assignee = new PendingExpenseAssignee { Name = "Jordan", ObjectId = "jordan-oid" };
            context.PendingExpenseAssignees.Add(assignee);
            await context.SaveChangesAsync();

            context.PendingExpenses.AddRange(
                new PendingExpense { Date = new DateTime(2026, 1, 1), Description = "Assigned", Amount = 100, AssigneeId = assignee.ID },
                new PendingExpense { Date = new DateTime(2026, 1, 2), Description = "Unassigned", Amount = 200 });
            await context.SaveChangesAsync();
            return assignee.ID;
        });

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new PendingExpensesController(context);

        var result = await controller.GetAll(search: null, assigneeId: assigneeId);

        await Assert.That(result.Length).IsEqualTo(1);
        await Assert.That(result[0].Description).IsEqualTo("Assigned");
        await Assert.That(result[0].AssigneeName).IsEqualTo("Jordan");
    }

    [Test]
    public async Task GetById_WithUnknownId_ReturnsNotFound()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new PendingExpensesController(context);

        var result = await controller.GetById(999);

        await Assert.That(result.Result).IsTypeOf<NotFoundResult>();
    }

    [Test]
    public async Task Update_SetsAssigneeAndNotes()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        var (pendingId, assigneeId) = await mocker.InDbScopeAsync(async context =>
        {
            var assignee = new PendingExpenseAssignee { Name = "Jordan", ObjectId = "jordan-oid" };
            var pending = new PendingExpense { Date = new DateTime(2026, 1, 1), Description = "Costco", Amount = 100 };
            context.PendingExpenseAssignees.Add(assignee);
            context.PendingExpenses.Add(pending);
            await context.SaveChangesAsync();
            return (pending.ID, assignee.ID);
        });

        using (var context = mocker.Get<BudgetWebContext>())
        {
            var controller = new PendingExpensesController(context);
            var result = await controller.Update(pendingId, new PendingExpenseUpdateRequest(assigneeId, "Needs review"));

            var dto = (result.Value as PendingExpenseDto) ?? ((OkObjectResult)result.Result!).Value as PendingExpenseDto;
            await Assert.That(dto!.AssigneeId).IsEqualTo(assigneeId);
            await Assert.That(dto.AssigneeName).IsEqualTo("Jordan");
            await Assert.That(dto.Notes).IsEqualTo("Needs review");
        }

        // Verify the change was actually persisted (not just reflected on the in-memory DTO).
        await mocker.InDbScopeAsync(async context =>
        {
            var pending = await context.PendingExpenses.SingleAsync(x => x.ID == pendingId);
            await Assert.That(pending.AssigneeId).IsEqualTo(assigneeId);
            await Assert.That(pending.Notes).IsEqualTo("Needs review");
        });
    }

    [Test]
    public async Task Update_CanClearAssignee()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        int pendingId = await mocker.InDbScopeAsync(async context =>
        {
            var assignee = new PendingExpenseAssignee { Name = "Jordan", ObjectId = "jordan-oid" };
            context.PendingExpenseAssignees.Add(assignee);
            var pending = new PendingExpense { Date = new DateTime(2026, 1, 1), Description = "Costco", Amount = 100 };
            context.PendingExpenses.Add(pending);
            await context.SaveChangesAsync();
            pending.AssigneeId = assignee.ID;
            await context.SaveChangesAsync();
            return pending.ID;
        });

        using (var context = mocker.Get<BudgetWebContext>())
        {
            var controller = new PendingExpensesController(context);
            var result = await controller.Update(pendingId, new PendingExpenseUpdateRequest(null, null));

            var dto = (result.Value as PendingExpenseDto) ?? ((OkObjectResult)result.Result!).Value as PendingExpenseDto;
            await Assert.That(dto!.AssigneeId).IsNull();
            await Assert.That(dto.AssigneeName).IsNull();
        }

        await mocker.InDbScopeAsync(async context =>
        {
            var pending = await context.PendingExpenses.SingleAsync(x => x.ID == pendingId);
            await Assert.That(pending.AssigneeId).IsNull();
        });
    }

    [Test]
    public async Task Update_WithUnknownAssignee_ReturnsBadRequest()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        int pendingId = await mocker.InDbScopeAsync(async context =>
        {
            var pending = new PendingExpense { Date = new DateTime(2026, 1, 1), Description = "Costco", Amount = 100 };
            context.PendingExpenses.Add(pending);
            await context.SaveChangesAsync();
            return pending.ID;
        });

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new PendingExpensesController(context);

        var result = await controller.Update(pendingId, new PendingExpenseUpdateRequest(999, null));

        await Assert.That(result.Result).IsTypeOf<BadRequestObjectResult>();
    }

    [Test]
    public async Task Update_WithUnknownPendingExpense_ReturnsNotFound()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new PendingExpensesController(context);

        var result = await controller.Update(999, new PendingExpenseUpdateRequest(null, "notes"));

        await Assert.That(result.Result).IsTypeOf<NotFoundResult>();
    }

    [Test]
    public async Task Delete_RemovesPendingExpense()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        int pendingId = await mocker.InDbScopeAsync(async context =>
        {
            var pending = new PendingExpense { Date = new DateTime(2026, 1, 1), Description = "Costco", Amount = 100 };
            context.PendingExpenses.Add(pending);
            await context.SaveChangesAsync();
            return pending.ID;
        });

        using (var context = mocker.Get<BudgetWebContext>())
        {
            var controller = new PendingExpensesController(context);
            var result = await controller.Delete(pendingId);
            await Assert.That(result).IsTypeOf<NoContentResult>();
        }

        await mocker.InDbScopeAsync(async context =>
        {
            await Assert.That(await context.PendingExpenses.CountAsync()).IsEqualTo(0);
        });
    }

    [Test]
    public async Task DeleteAll_WithNoFilters_RemovesAllPendingExpenses()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        await mocker.InDbScopeAsync(async context =>
        {
            context.PendingExpenses.AddRange(
                new PendingExpense { Date = new DateTime(2026, 1, 1), Description = "Costco", Amount = 100 },
                new PendingExpense { Date = new DateTime(2026, 1, 2), Description = "Gas station", Amount = 200 });
            await context.SaveChangesAsync();
        });

        using (var context = mocker.Get<BudgetWebContext>())
        {
            var controller = new PendingExpensesController(context);
            var result = await controller.DeleteAll(search: null, assigneeId: null);
            await Assert.That(result).IsTypeOf<NoContentResult>();
        }

        await mocker.InDbScopeAsync(async context =>
        {
            await Assert.That(await context.PendingExpenses.CountAsync()).IsEqualTo(0);
        });
    }

    [Test]
    public async Task DeleteAll_WithSearchFilter_OnlyRemovesMatchingPendingExpenses()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        await mocker.InDbScopeAsync(async context =>
        {
            context.PendingExpenses.AddRange(
                new PendingExpense { Date = new DateTime(2026, 1, 1), Description = "Costco groceries", Amount = 100 },
                new PendingExpense { Date = new DateTime(2026, 1, 2), Description = "Gas station", Amount = 200 });
            await context.SaveChangesAsync();
        });

        using (var context = mocker.Get<BudgetWebContext>())
        {
            var controller = new PendingExpensesController(context);
            await controller.DeleteAll(search: "Costco", assigneeId: null);
        }

        await mocker.InDbScopeAsync(async context =>
        {
            var remaining = await context.PendingExpenses.ToListAsync();
            await Assert.That(remaining.Count).IsEqualTo(1);
            await Assert.That(remaining[0].Description).IsEqualTo("Gas station");
        });
    }

    [Test]
    public async Task DeleteAll_WithAssigneeFilter_OnlyRemovesMatchingPendingExpenses()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        int assigneeId = await mocker.InDbScopeAsync(async context =>
        {
            var assignee = new PendingExpenseAssignee { Name = "Jordan" };
            context.PendingExpenseAssignees.Add(assignee);
            await context.SaveChangesAsync();

            context.PendingExpenses.AddRange(
                new PendingExpense { Date = new DateTime(2026, 1, 1), Description = "Assigned", Amount = 100, AssigneeId = assignee.ID },
                new PendingExpense { Date = new DateTime(2026, 1, 2), Description = "Unassigned", Amount = 200 });
            await context.SaveChangesAsync();
            return assignee.ID;
        });

        using (var context = mocker.Get<BudgetWebContext>())
        {
            var controller = new PendingExpensesController(context);
            await controller.DeleteAll(search: null, assigneeId: assigneeId);
        }

        await mocker.InDbScopeAsync(async context =>
        {
            var remaining = await context.PendingExpenses.ToListAsync();
            await Assert.That(remaining.Count).IsEqualTo(1);
            await Assert.That(remaining[0].Description).IsEqualTo("Unassigned");
        });
    }

    [Test]
    public async Task DeleteAll_WithMonthFilter_OnlyRemovesMatchingPendingExpenses()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        await mocker.InDbScopeAsync(async context =>
        {
            context.PendingExpenses.AddRange(
                new PendingExpense { Date = new DateTime(2026, 1, 10), Description = "January", Amount = 100 },
                new PendingExpense { Date = new DateTime(2026, 2, 10), Description = "February", Amount = 200 });
            await context.SaveChangesAsync();
        });

        using (var context = mocker.Get<BudgetWebContext>())
        {
            var controller = new PendingExpensesController(context);
            await controller.DeleteAll(month: new DateTime(2026, 1, 1), search: null, assigneeId: null);
        }

        await mocker.InDbScopeAsync(async context =>
        {
            var remaining = await context.PendingExpenses.ToListAsync();
            await Assert.That(remaining.Count).IsEqualTo(1);
            await Assert.That(remaining[0].Description).IsEqualTo("February");
        });
    }

    [Test]
    public async Task Convert_WithNoItems_ReturnsBadRequest()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        int pendingId = await mocker.InDbScopeAsync(async context =>
        {
            var pending = new PendingExpense { Date = new DateTime(2026, 1, 1), Description = "Costco", Amount = 100 };
            context.PendingExpenses.Add(pending);
            await context.SaveChangesAsync();
            return pending.ID;
        });

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new PendingExpensesController(context);

        var result = await controller.Convert(pendingId, new ConvertPendingExpenseRequest("Costco", DateTime.Today, []));

        await Assert.That(result).IsTypeOf<BadRequestObjectResult>();
    }

    [Test]
    public async Task Convert_WithUnknownId_ReturnsNotFound()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        using var context = mocker.Get<BudgetWebContext>();
        var controller = new PendingExpensesController(context);

        var result = await controller.Convert(999, new ConvertPendingExpenseRequest("Costco", DateTime.Today,
            [new ConvertPendingExpenseItemRequest(1, 100)]));

        await Assert.That(result).IsTypeOf<NotFoundResult>();
    }

    [Test]
    public async Task Convert_Debit_CreatesExpenseTransactionAndRemovesPendingExpense()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        using (var context = mocker.Get<BudgetWebContext>())
        {
            var category = new ExpenseCategory { Name = "Groceries", CurrentBalance = 1_000_00 };
            context.ExpenseCategories.Add(category);
            var pending = new PendingExpense
            {
                Date = new DateTime(2026, 1, 5),
                Description = "Costco",
                Amount = 45_00,
                IsDebit = true
            };
            context.PendingExpenses.Add(pending);
            await context.SaveChangesAsync();

            var controller = new PendingExpensesController(context);
            var result = await controller.Convert(pending.ID, new ConvertPendingExpenseRequest(
                "Costco", new DateTime(2026, 1, 5), [new ConvertPendingExpenseItemRequest(category.ID, 45_00)]));

            await Assert.That(result).IsTypeOf<StatusCodeResult>();
            await Assert.That(((StatusCodeResult)result).StatusCode).IsEqualTo(201);
        }

        await mocker.InDbScopeAsync(async context =>
        {
            await Assert.That(await context.PendingExpenses.CountAsync()).IsEqualTo(0);

            var item = await context.ExpenseCategoryItems
                .Include(x => x.Details)
                .SingleAsync();
            await Assert.That(item.Description).IsEqualTo("Costco");
            await Assert.That(item.Details!.Single().Amount).IsEqualTo(-45_00);

            var category = await context.ExpenseCategories.SingleAsync();
            await Assert.That(category.CurrentBalance).IsEqualTo(1_000_00 - 45_00);
        });
    }

    [Test]
    public async Task Convert_WithIgnoreBudget_SetsConvertedItemDetailsToIgnoreBudget()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        using (var context = mocker.Get<BudgetWebContext>())
        {
            var category = new ExpenseCategory { Name = "Groceries", CurrentBalance = 1_000_00 };
            context.ExpenseCategories.Add(category);
            var pending = new PendingExpense
            {
                Date = new DateTime(2026, 1, 5),
                Description = "Costco",
                Amount = 45_00,
                IsDebit = true
            };
            context.PendingExpenses.Add(pending);
            await context.SaveChangesAsync();

            var controller = new PendingExpensesController(context);
            await controller.Convert(pending.ID, new ConvertPendingExpenseRequest(
                "Costco",
                new DateTime(2026, 1, 5),
                [new ConvertPendingExpenseItemRequest(category.ID, 45_00)],
                IgnoreBudget: true));
        }

        await mocker.InDbScopeAsync(async context =>
        {
            var item = await context.ExpenseCategoryItems
                .Include(x => x.Details)
                .SingleAsync();
            await Assert.That(item.Details!.Single().IgnoreBudget).IsTrue();
        });
    }

    [Test]
    public async Task Convert_Debit_CanSplitAcrossMultipleCategories()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        using (var context = mocker.Get<BudgetWebContext>())
        {
            var groceries = new ExpenseCategory { Name = "Groceries", CurrentBalance = 500_00 };
            var household = new ExpenseCategory { Name = "Household", CurrentBalance = 200_00 };
            context.ExpenseCategories.AddRange(groceries, household);
            var pending = new PendingExpense
            {
                Date = new DateTime(2026, 1, 5),
                Description = "Costco",
                Amount = 100_00,
                IsDebit = true
            };
            context.PendingExpenses.Add(pending);
            await context.SaveChangesAsync();

            var controller = new PendingExpensesController(context);
            await controller.Convert(pending.ID, new ConvertPendingExpenseRequest(
                "Costco", new DateTime(2026, 1, 5),
                [
                    new ConvertPendingExpenseItemRequest(groceries.ID, 70_00),
                    new ConvertPendingExpenseItemRequest(household.ID, 30_00),
                ]));
        }

        await mocker.InDbScopeAsync(async context =>
        {
            var item = await context.ExpenseCategoryItems
                .Include(x => x.Details)
                .SingleAsync();
            await Assert.That(item.Details!.Count).IsEqualTo(2);
            await Assert.That(item.Details!.Sum(x => x.Amount)).IsEqualTo(-100_00);

            var groceries = await context.ExpenseCategories.SingleAsync(x => x.Name == "Groceries");
            var household = await context.ExpenseCategories.SingleAsync(x => x.Name == "Household");
            await Assert.That(groceries.CurrentBalance).IsEqualTo(500_00 - 70_00);
            await Assert.That(household.CurrentBalance).IsEqualTo(200_00 - 30_00);
        });
    }

    [Test]
    public async Task Convert_Credit_CreatesIncomeTransaction()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        using (var context = mocker.Get<BudgetWebContext>())
        {
            var category = new ExpenseCategory { Name = "Refunds", CurrentBalance = 0 };
            context.ExpenseCategories.Add(category);
            var pending = new PendingExpense
            {
                Date = new DateTime(2026, 1, 5),
                Description = "Refund",
                Amount = 20_00,
                IsDebit = false
            };
            context.PendingExpenses.Add(pending);
            await context.SaveChangesAsync();

            var controller = new PendingExpensesController(context);
            await controller.Convert(pending.ID, new ConvertPendingExpenseRequest(
                "Refund", new DateTime(2026, 1, 5), [new ConvertPendingExpenseItemRequest(category.ID, 20_00)]));
        }

        await mocker.InDbScopeAsync(async context =>
        {
            var item = await context.ExpenseCategoryItems
                .Include(x => x.Details)
                .SingleAsync();
            await Assert.That(item.Details!.Single().Amount).IsEqualTo(20_00);

            var category = await context.ExpenseCategories.SingleAsync();
            await Assert.That(category.CurrentBalance).IsEqualTo(20_00);
        });
    }

    [Test]
    public async Task Convert_WithIgnoreBudget_StoresConvertedItemAsIgnoredByBudget()
    {
        AutoMocker mocker = new();
        mocker.WithDbContext<BudgetWebContext>();

        using (var context = mocker.Get<BudgetWebContext>())
        {
            var category = new ExpenseCategory { Name = "Groceries", CurrentBalance = 200_00 };
            context.ExpenseCategories.Add(category);
            var pending = new PendingExpense
            {
                Date = new DateTime(2026, 1, 6),
                Description = "Costco",
                Amount = 50_00,
                IsDebit = true
            };
            context.PendingExpenses.Add(pending);
            await context.SaveChangesAsync();

            var controller = new PendingExpensesController(context);
            await controller.Convert(
                pending.ID,
                new ConvertPendingExpenseRequest(
                    "Costco",
                    new DateTime(2026, 1, 6),
                    [new ConvertPendingExpenseItemRequest(category.ID, 50_00)],
                    IgnoreBudget: true));
        }

        await mocker.InDbScopeAsync(async context =>
        {
            var item = await context.ExpenseCategoryItems
                .Include(x => x.Details)
                .SingleAsync();
            await Assert.That(item.IgnoreBudget).IsTrue();
            await Assert.That(item.Details!.All(x => x.IgnoreBudget)).IsTrue();
        });
    }
}
