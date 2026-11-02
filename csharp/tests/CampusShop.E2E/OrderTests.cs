using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace CampusShop.E2E;

/// <summary>
/// 下单流程 E2E。继承 PageTest 后每个 [Test] 都拿到全新的浏览器上下文，
/// 用例自己登录，不依赖其他用例执行顺序，也不依赖已有购物车内容。
/// </summary>
public class OrderTests : TracedPageTest
{
    private const string BaseUrl = "http://localhost:5000";

    // 需求 2.1：满 100 减 10。笔记本单价 50.00，买 2 件。
    private const decimal NotebookPrice = 50.00m;
    private const int Quantity = 2;
    private const decimal DiscountThreshold = 100.00m;
    private const decimal DiscountAmount = 10.00m;

    private static readonly decimal ExpectedSubtotal = NotebookPrice * Quantity;
    private static readonly decimal ExpectedTotal =
        ExpectedSubtotal >= DiscountThreshold ? ExpectedSubtotal - DiscountAmount : ExpectedSubtotal;

    private static string Yen(decimal amount) =>
        "¥" + amount.ToString("0.00", CultureInfo.InvariantCulture);

    [Test]
    public async Task Order_TwoNotebooks_ShowsOrderNumber()
    {
        // 1. 登录：本用例自己登录，不依赖其他用例。
        await Page.GotoAsync(BaseUrl + "/");
        await Page.GetByLabel("用户名").FillAsync("alice");
        await Page.GetByLabel("密码").FillAsync("123456");
        await Page.GetByRole(AriaRole.Button, new() { Name = "登录" }).ClickAsync();
        await Expect(Page.GetByText("欢迎，alice")).ToBeVisibleAsync();

        // 2. 商品列表：按“笔记本”定位到对应行，再在该行内改数量、加入购物车。
        //    六行都有同名的“数量”输入框和“加入购物车”按钮，必须先定位行再定位控件。
        var notebookRow = Page.GetByRole(AriaRole.Row)
            .Filter(new() { Has = Page.GetByRole(AriaRole.Cell, new() { Name = "笔记本", Exact = true }) });
        await Expect(notebookRow).ToHaveCountAsync(1);

        await notebookRow.GetByRole(AriaRole.Spinbutton, new() { Name = "数量" })
            .FillAsync(Quantity.ToString(CultureInfo.InvariantCulture));
        await notebookRow.GetByRole(AriaRole.Button, new() { Name = "加入购物车" }).ClickAsync();

        await Expect(Page.GetByTestId("message"))
            .ToHaveTextAsync($"已将 {Quantity} 件笔记本加入购物车");

        // 3. 购物车：按需求核对金额。
        //    原价 = 50.00 × 2 = 100.00；达到 100，减 10；应付 = 90.00。
        await Page.GetByRole(AriaRole.Link, new() { Name = "购物车" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex(@"/Cart$"));
        await Expect(Page.GetByText($"笔记本 × {Quantity}")).ToBeVisibleAsync();
        await Expect(Page.GetByText("原价合计：" + Yen(ExpectedSubtotal))).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("cart-total")).ToHaveTextAsync(Yen(ExpectedTotal));

        // 4. 下单：订单号格式为 CS + 数字，且下单后购物车清空。
        await Page.GetByRole(AriaRole.Button, new() { Name = "下单" }).ClickAsync();

        var orderNumber = Page.GetByTestId("order-no");
        await Expect(orderNumber).ToBeVisibleAsync();
        await Expect(orderNumber).ToHaveTextAsync(new Regex(@"^CS\d+$"));
        await Expect(Page.GetByText("购物车为空")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("cart-total")).ToHaveTextAsync(Yen(0m));
    }
}
