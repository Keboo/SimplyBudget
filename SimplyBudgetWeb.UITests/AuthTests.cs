namespace SimplyBudgetWeb.UITests;

public class AuthTests : UITestBase
{
    [Test]
    public async Task AnonymousUserSeesSignInAction()
    {
        await Page.GotoAsync(FrontendBaseUri.ToString());
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign in" })
            .WaitForAsync(new() { Timeout = PlaywrightConfiguration.DefaultTimeout });
        await Assert.That(await Page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).IsVisibleAsync())
            .IsTrue();
    }

    [Test]
    public async Task AnonymousUserDoesNotSeeAuthenticatedNavigation()
    {
        await Page.GotoAsync(FrontendBaseUri.ToString());
        await Assert.That(await Page.GetByRole(AriaRole.Button, new() { Name = "Budget" }).CountAsync()).IsEqualTo(0);
        await Assert.That(await Page.GetByRole(AriaRole.Button, new() { Name = "History" }).CountAsync()).IsEqualTo(0);
        await Assert.That(await Page.GetByRole(AriaRole.Button, new() { Name = "Sign out" }).CountAsync()).IsEqualTo(0);
    }

    [Test]
    [Category(TestCategories.Accessibility)]
    public async Task LandingPageIsAccessible()
    {
        await Page.GotoAsync(FrontendBaseUri.ToString());
        await AssertNoAccessibilityViolations();
    }
}

