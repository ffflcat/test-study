using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace CampusShop.E2E;

/// <summary>
/// 登录冒烟用例。继承 PageTest 后每个 [Test] 都拿到全新的浏览器上下文，
/// 用例之间不共享 Cookie 或页面状态。
/// </summary>
public class LoginTests : TracedPageTest
{
    private const string BaseUrl = "http://localhost:5000";

    [Test]
    public async Task Login_Alice_Success()
    {
        await Page.GotoAsync(BaseUrl + "/");
        await Page.GetByLabel("用户名").FillAsync("alice");
        await Page.GetByLabel("密码").FillAsync("123456");
        await Page.GetByRole(AriaRole.Button, new() { Name = "登录" }).ClickAsync();

        await Expect(Page.GetByText("欢迎，alice")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Login_WrongPassword_ShowsError()
    {
        await Page.GotoAsync(BaseUrl + "/");
        await Page.GetByLabel("用户名").FillAsync("alice");
        await Page.GetByLabel("密码").FillAsync("wrong-password");
        await Page.GetByRole(AriaRole.Button, new() { Name = "登录" }).ClickAsync();

        // 断言页面真正给出了错误提示，而不只是网址还停在登录页。
        var error = Page.GetByTestId("error-msg");
        await Expect(error).ToBeVisibleAsync();
        await Expect(error).ToHaveTextAsync("用户名或密码错误");

        // 同时确认没有登录成功：顶部欢迎语和导航都不应出现。
        await Expect(Page.GetByText("欢迎，alice")).ToHaveCountAsync(0);
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "购物车" })).ToHaveCountAsync(0);
    }
}
