using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace CampusShop.E2E;

/// <summary>
/// 购物车 E2E。PageTest 为每个 [Test] 提供全新的浏览器上下文，
/// 用例自己登录，不依赖其他用例。
/// </summary>
public class CartTests : PageTest
{
    private const string BaseUrl = "http://localhost:5000";

    [Test]
    public async Task Cart_Empty_ShowsEmptyStateAndDisabledCheckout()
    {
        // 1. 用 bob 登录（种子数据里 bob 的购物车为空）。
        await Page.GotoAsync(BaseUrl + "/");
        await Page.GetByLabel("用户名").FillAsync("bob");
        await Page.GetByLabel("密码").FillAsync("123456");
        await Page.GetByRole(AriaRole.Button, new() { Name = "登录" }).ClickAsync();
        await Expect(Page.GetByText("欢迎，bob")).ToBeVisibleAsync();

        // 2. 进入购物车。
        await Page.GetByRole(AriaRole.Link, new() { Name = "购物车" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex(@"/Cart$"));

        // 3. 需求 4.10：购物车为空时显示提示，且“下单”按钮不可点击。
        await Expect(Page.GetByText("购物车为空")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "下单" })).ToBeDisabledAsync();
    }
}
