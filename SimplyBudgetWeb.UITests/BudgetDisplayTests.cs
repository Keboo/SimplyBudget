using System.Text.Json;

namespace SimplyBudgetWeb.UITests;

public class BudgetDisplayTests : UITestBase
{
    [Test]
    [Arguments(390)]
    [Arguments(1440)]
    public async Task CategoryActionsEditAndPrepareTransfersWithoutPostingAutomatically(int width)
    {
        await Page.SetViewportSizeAsync(width, 900);
        await Page.RouteAsync("**/src/stores/auth.ts*", route => route.FulfillAsync(new()
        {
            ContentType = "application/javascript",
            Body = """
                export const useAuthStore = () => ({
                  isAuthenticated: true, displayName: 'Budget reviewer',
                  initialize: async () => {}, getToken: async () => '',
                  login: () => {}, logout: () => {}
                });
                """,
        }));
        await Page.RouteAsync("**/src/services/monthUpdatesHub.ts*", route => route.FulfillAsync(new()
        {
            ContentType = "application/javascript",
            Body = """
                export const createMonthUpdatesHubClient = () => ({
                  start: async () => {}, stop: async () => {}, setMonth: async () => {}
                });
                """,
        }));
        var categories = new[]
        {
            JsonSerializer.SerializeToNode(Category(1, "Groceries", -1234, 25000, false))!,
            JsonSerializer.SerializeToNode(Category(2, "Savings", 0, 0, false))!,
            JsonSerializer.SerializeToNode(Category(3, "Travel", 700, 0, false))!,
        };
        var transfers = new List<string>();
        var edits = new List<string>();
        await Page.RouteAsync("**/api/**", async route =>
        {
            var path = new Uri(route.Request.Url).AbsolutePath;
            object response = Array.Empty<object>();
            if (route.Request.Method == "POST")
            {
                transfers.Add(route.Request.PostData!);
                response = new { };
            }
            else if (route.Request.Method == "PUT")
            {
                edits.Add(route.Request.PostData!);
                using var edit = JsonDocument.Parse(route.Request.PostData!);
                categories[0]["name"] = edit.RootElement.GetProperty("name").GetString();
                response = new { };
            }
            else if (path == "/api/budget")
            {
                response = new { totalBudget = 30000, totalAccountAmount = 50000, estimatedMonthlyIncome = 40000, month = "2026-10", categories };
            }
            else if (path == "/api/expense-categories")
            {
                response = categories;
            }
            await route.FulfillAsync(new() { ContentType = "application/json", Body = JsonSerializer.Serialize(response) });
        });
        await Page.GotoAsync(new Uri(FrontendBaseUri, "budget?month=2026-10").ToString());

        var actions = Page.Locator("[data-category-actions='1']");
        await actions.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Page.GetByRole(AriaRole.Menuitem, new() { Name = "Create transfer" }).ClickAsync();
        var dialog = Page.GetByRole(AriaRole.Dialog);
        await Assertions.Expect(dialog.GetByRole(AriaRole.Combobox, new() { Name = "To Category", Exact = true })).ToHaveValueAsync("Groceries");
        await Assertions.Expect(dialog.GetByRole(AriaRole.Combobox, new() { Name = "From Category", Exact = true })).ToHaveValueAsync("");
        await Assertions.Expect(dialog.GetByLabel("Amount ($)", new() { Exact = true })).ToHaveValueAsync("12.34");
        var save = dialog.GetByRole(AriaRole.Button, new() { Name = "Save", Exact = true });
        await Assertions.Expect(save).ToBeDisabledAsync();
        await Assert.That(transfers.Count).IsEqualTo(0);
        await dialog.GetByRole(AriaRole.Combobox, new() { Name = "From Category", Exact = true }).FillAsync("Travel");
        await Page.GetByRole(AriaRole.Option, new() { Name = "Travel", Exact = true }).ClickAsync();
        await Assertions.Expect(save).ToBeEnabledAsync();
        await Assert.That(transfers.Count).IsEqualTo(0);
        await save.ClickAsync();
        await Assertions.Expect(dialog).ToBeHiddenAsync();
        await Assertions.Expect(actions).ToBeFocusedAsync();
        await Assert.That(transfers.Count).IsEqualTo(1);
        using (var transfer = JsonDocument.Parse(transfers[0]))
        {
            await Assert.That(transfer.RootElement.GetProperty("amount").GetInt32()).IsEqualTo(1234);
            await Assert.That(transfer.RootElement.GetProperty("fromCategoryId").GetInt32()).IsEqualTo(3);
            await Assert.That(transfer.RootElement.GetProperty("toCategoryId").GetInt32()).IsEqualTo(1);
        }

        foreach (var (id, name) in new[] { (2, "Savings"), (3, "Travel") })
        {
            await Page.Locator($"[data-category-actions='{id}']").ClickAsync();
            await Page.GetByRole(AriaRole.Menuitem, new() { Name = "Create transfer" }).ClickAsync();
            await Assertions.Expect(dialog.GetByRole(AriaRole.Combobox, new() { Name = "To Category", Exact = true })).ToHaveValueAsync(name);
            await Assertions.Expect(dialog.GetByRole(AriaRole.Combobox, new() { Name = "From Category", Exact = true })).ToHaveValueAsync("");
            await Assertions.Expect(dialog.GetByLabel("Amount ($)", new() { Exact = true })).ToHaveValueAsync("");
            await dialog.GetByLabel("Amount ($)", new() { Exact = true }).FillAsync("88");
            await dialog.GetByRole(AriaRole.Button, new() { Name = "Cancel", Exact = true }).ClickAsync();
            await Assertions.Expect(dialog).ToBeHiddenAsync();
        }
        await Assert.That(transfers.Count).IsEqualTo(1);

        await actions.ClickAsync();
        await Page.GetByRole(AriaRole.Menuitem, new() { Name = "Edit category" }).ClickAsync();
        await Assertions.Expect(dialog).ToHaveAccessibleNameAsync("Edit expense category");
        await dialog.GetByLabel("Name", new() { Exact = true }).FillAsync("Discard this");
        await Page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(dialog).ToBeHiddenAsync();
        await Assertions.Expect(actions).ToBeFocusedAsync();
        await Assert.That(edits.Count).IsEqualTo(0);
        await actions.ClickAsync();
        await Page.GetByRole(AriaRole.Menuitem, new() { Name = "Edit category" }).ClickAsync();
        await Assertions.Expect(dialog.GetByLabel("Name", new() { Exact = true })).ToHaveValueAsync("Groceries");
        await dialog.GetByLabel("Name", new() { Exact = true }).FillAsync("Updated groceries");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Save", Exact = true }).ClickAsync();
        await Assertions.Expect(dialog).ToBeHiddenAsync();
        await Assertions.Expect(Page.Locator(".budget-category-name").Filter(new() { HasText = "Updated groceries" })).ToBeVisibleAsync();
        await Assert.That(edits.Count).IsEqualTo(1);
        await Assertions.Expect(Page).ToHaveURLAsync(new Regex("/budget\\?month=2026-10$"));
        await Assert.That(await Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth")).IsTrue();
    }

    [Test]
    [Arguments(320, "light")]
    [Arguments(390, "light")]
    [Arguments(768, "light")]
    [Arguments(1440, "light")]
    [Arguments(390, "dark")]
    [Arguments(1440, "dark")]
    public async Task ExpenseRowsShowMonthlyAllocationAndDiscloseAverages(int width, string theme)
    {
        await Page.SetViewportSizeAsync(width, 900);
        await Page.AddInitScriptAsync($"localStorage.setItem('SimplyBudgetWebTheme', '{theme}')");

        // Keep this layout test independent of Entra sign-in and real financial data.
        await Page.RouteAsync("**/src/stores/auth.ts*", route => route.FulfillAsync(new()
        {
            ContentType = "application/javascript",
            Body = """
                export const useAuthStore = () => ({
                  isAuthenticated: true, displayName: 'Budget reviewer',
                  initialize: async () => {}, getToken: async () => '',
                  login: () => {}, logout: () => {}
                });
                """,
        }));
        await Page.RouteAsync("**/src/services/monthUpdatesHub.ts*", route => route.FulfillAsync(new()
        {
            ContentType = "application/javascript",
            Body = """
                export const createMonthUpdatesHubClient = () => ({
                  start: async () => {}, stop: async () => {}, setMonth: async () => {}
                });
                """,
        }));
        await Page.RouteAsync("**/api/**", route =>
        {
            var uri = new Uri(route.Request.Url);
            var november = uri.Query.Contains("2026-11", StringComparison.Ordinal);
            object response = uri.AbsolutePath switch
            {
                "/api/budget" => new
                {
                    totalBudget = 30000,
                    totalAccountAmount = 50000,
                    estimatedMonthlyIncome = 40000,
                    month = november ? "2026-11" : "2026-10",
                    categories = new[]
                    {
                        Category(1, "Groceries", 12500, november ? 42000 : 25000, false),
                        Category(2, new string('W', 90), -123456789, 0, true),
                    },
                },
                "/api/expense-categories/1/monthly-expenses" => new
                {
                    name = "Groceries",
                    budgetedAmount = 30000,
                    usePercentage = false,
                    budgetedPercentage = 0,
                    months = Enumerable.Range(0, 12).Select(index => new
                    {
                        month = new DateTime(2025, 10, 1).AddMonths(index).ToString("yyyy-MM"),
                        amount = 17500,
                    }),
                },
                _ => Array.Empty<object>(),
            };
            return route.FulfillAsync(new() { ContentType = "application/json", Body = JsonSerializer.Serialize(response) });
        });

        await Page.GotoAsync(new Uri(FrontendBaseUri, "budget?month=2026-10").ToString());
        var rows = Page.Locator(".category-clickable-row");
        var groceries = rows.Filter(new() { Has = Page.GetByText("Groceries", new() { Exact = true }) });
        await Assertions.Expect(groceries.Locator(".budget-category-heading")).ToHaveTextAsync("Groceries Balance: $125.00");
        await Assertions.Expect(groceries.GetByText("Allocated: $250.00", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(rows.Nth(1).GetByText("Allocated: $0.00", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(rows.Nth(1).GetByText("Budget: 15%", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(rows.Nth(1).GetByText("Balance: -$1,234,567.89", new() { Exact = true })).ToBeVisibleAsync();

        var nameBox = (await groceries.Locator(".budget-category-name").BoundingBoxAsync())!;
        var balanceBox = (await groceries.Locator(".budget-category-heading .v-chip").BoundingBoxAsync())!;
        await Assert.That(Math.Abs(nameBox.Y + nameBox.Height / 2 - balanceBox.Y - balanceBox.Height / 2) < 2).IsTrue();

        var averages = groceries.GetByText("3mo avg: $350.00", new() { Exact = true });
        var toggle = groceries.GetByRole(AriaRole.Button, new() { Name = "Show spending averages" });
        await Assertions.Expect(groceries.Locator("details")).ToHaveCountAsync(0);
        await Assertions.Expect(averages).ToBeHiddenAsync();
        await Assertions.Expect(toggle).ToHaveAttributeAsync("aria-expanded", "false");

        // The toggle must not make the row taller than its chips, yet still offer a 44px touch target.
        var spentChip = groceries.Locator(".v-chip", new() { HasText = "Spent: $175.00" });
        var spentBox = (await spentChip.BoundingBoxAsync())!;
        var toggleBox = (await toggle.BoundingBoxAsync())!;
        await Assert.That(toggleBox.Height <= spentBox.Height + 1).IsTrue();
        await Assert.That(await toggle.EvaluateAsync<bool>("""
            button => {
              const box = button.getBoundingClientRect();
              const x = box.left + box.width / 2, y = box.top + box.height / 2;
              return [[x, y - 21], [x, y + 21], [x - 21, y], [x + 21, y]]
                .every(([px, py]) => button.contains(document.elementFromPoint(px, py)));
            }
            """)).IsTrue();

        await toggle.ClickAsync();
        var hideToggle = groceries.GetByRole(AriaRole.Button, new() { Name = "Hide spending averages" });
        await Assertions.Expect(hideToggle).ToHaveAttributeAsync("aria-expanded", "true");
        await Assertions.Expect(averages).ToBeVisibleAsync();
        await Assertions.Expect(groceries.GetByText("6mo avg: $280.00", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(groceries.GetByText("12mo avg: $260.00", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(Page).ToHaveURLAsync(new Regex("/budget\\?month=2026-10$"));
        var averageBox = (await groceries.Locator(".v-chip", new() { HasText = "3mo avg: $350.00" }).BoundingBoxAsync())!;
        var spentAfterBox = (await spentChip.BoundingBoxAsync())!;
        if (width >= 1440)
        {
            await Assert.That(Math.Abs(averageBox.Y - spentAfterBox.Y) < 2).IsTrue();
        }
        else
        {
            await Assert.That(averageBox.Y >= spentAfterBox.Y - 1).IsTrue();
        }

        await hideToggle.ClickAsync();
        await Assertions.Expect(averages).ToBeHiddenAsync();
        await toggle.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(averages).ToBeVisibleAsync();
        await Page.Keyboard.PressAsync("Space");
        await Assertions.Expect(averages).ToBeHiddenAsync();
        await Assertions.Expect(Page).ToHaveURLAsync(new Regex("/budget\\?month=2026-10$"));

        await rows.Nth(1).GetByRole(AriaRole.Button, new() { Name = "Show spending averages" }).ClickAsync();
        await Assertions.Expect(rows.Nth(1).GetByText("12mo avg: $260.00", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(averages).ToBeHiddenAsync();
        await Assert.That(await Page.EvaluateAsync<bool>("""
            () => document.documentElement.scrollWidth <= innerWidth
              && [...document.querySelectorAll('.budget-category-content, .budget-category-content .v-chip')]
                .every(element => {
                  const box = element.getBoundingClientRect();
                  return box.left >= 0 && box.right <= innerWidth + 1
                    && element.scrollWidth <= element.clientWidth + 1;
                })
            """)).IsTrue();
        var chartButton = groceries.GetByRole(AriaRole.Button, new() { Name = "Open category chart" });
        var chartBox = (await chartButton.BoundingBoxAsync())!;
        await Assert.That(chartBox.Width >= 44 && chartBox.Height >= 44).IsTrue();

        await chartButton.ClickAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Dialog)).ToContainTextAsync("Groceries spending (last 12 months)");
        await Assertions.Expect(Page).ToHaveURLAsync(new Regex("/budget\\?month=2026-10$"));
        await Page.GetByRole(AriaRole.Button, new() { Name = "Close", Exact = true }).ClickAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Next month" }).ClickAsync();
        await Assertions.Expect(groceries.GetByTitle("Allocated in November 2026")).ToHaveTextAsync("Allocated: $420.00");
        await groceries.Locator(".budget-category-name").ClickAsync();
        await Assertions.Expect(Page).ToHaveURLAsync(new Regex("/history\\?.*categoryId=1"));
    }

    private static object Category(int id, string name, int balance, int allocation, bool percentage) => new
    {
        id,
        name,
        description = "Household essentials with a description that can wrap on narrow screens.",
        categoryName = "Essentials",
        accountId = (int?)null,
        cap = (int?)null,
        isHidden = false,
        currentBalance = balance,
        budgetedAmount = 30000,
        budgetedPercentage = percentage ? 15 : 0,
        usePercentage = percentage,
        monthlyAllocations = allocation,
        monthlyExpenses = 17500,
        threeMonthAverage = 35000,
        sixMonthAverage = 28000,
        twelveMonthAverage = 26000,
    };
}