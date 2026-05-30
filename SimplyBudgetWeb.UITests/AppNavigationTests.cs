namespace SimplyBudgetWeb.UITests;

public class AppNavigationTests : UITestBase
{
    [Test]
    public async Task AnonymousUserCanLoadCoreRoutes()
    {
        var routes = new[] { "budget", "history", "accounts", "settings", "import" };

        foreach (var route in routes)
        {
            await Page.GotoAsync(new Uri(FrontendBaseUri, route).ToString());
            await Page.GetByRole(AriaRole.Button, new() { Name = "Sign in" })
                .WaitForAsync(new() { Timeout = PlaywrightConfiguration.DefaultTimeout });
            await Assert.That(Page.Url).Contains($"/{route}");
        }
    }
}
