namespace SimplyBudgetWeb.UITests;

public class HomePageTests : UITestBase
{
    [Test]
    public async Task LandingRouteRedirectsToBudget()
    {
        await Page.GotoAsync(FrontendBaseUri.ToString());
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign in" })
            .WaitForAsync(new() { Timeout = PlaywrightConfiguration.DefaultTimeout });
        await Assert.That(Page.Url).Contains("/budget");
    }

    [Test]
    public async Task HeaderBrandIsVisible()
    {
        await Page.GotoAsync(FrontendBaseUri.ToString());
        await Assert.That(await Page.GetByText("Simply Budget", new() { Exact = true }).IsVisibleAsync())
            .IsTrue();
    }
}
