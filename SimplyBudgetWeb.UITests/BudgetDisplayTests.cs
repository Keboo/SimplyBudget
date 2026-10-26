using System.Text.Json;

namespace SimplyBudgetWeb.UITests;

public class BudgetDisplayTests : UITestBase
{
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
        var summary = groceries.Locator("summary");
        await Assertions.Expect(averages).ToBeHiddenAsync();
        await summary.ClickAsync();
        await Assertions.Expect(averages).ToBeVisibleAsync();
        await Assertions.Expect(groceries.GetByText("6mo avg: $280.00", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(groceries.GetByText("12mo avg: $260.00", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(Page).ToHaveURLAsync(new Regex("/budget\\?month=2026-10$"));
        await summary.ClickAsync();
        await Assertions.Expect(averages).ToBeHiddenAsync();
        await summary.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(averages).ToBeVisibleAsync();
        await Page.Keyboard.PressAsync("Space");
        await Assertions.Expect(averages).ToBeHiddenAsync();
        await Assertions.Expect(Page).ToHaveURLAsync(new Regex("/budget\\?month=2026-10$"));

        await rows.Nth(1).Locator("summary").ClickAsync();
        await Assert.That(await Page.EvaluateAsync<bool>("""
            () => document.documentElement.scrollWidth <= innerWidth
              && [...document.querySelectorAll('.budget-category-content, .budget-category-content .v-chip')]
                .every(element => {
                  const box = element.getBoundingClientRect();
                  return box.left >= 0 && box.right <= innerWidth + 1
                    && element.scrollWidth <= element.clientWidth + 1;
                })
            """)).IsTrue();
        var summaryBox = (await summary.BoundingBoxAsync())!;
        var chartButton = groceries.GetByRole(AriaRole.Button, new() { Name = "Open category chart" });
        var chartBox = (await chartButton.BoundingBoxAsync())!;
        await Assert.That(summaryBox.Height >= 44 && chartBox.Width >= 44 && chartBox.Height >= 44).IsTrue();

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