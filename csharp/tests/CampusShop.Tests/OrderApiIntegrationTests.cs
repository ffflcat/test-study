using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CampusShop.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CampusShop.Tests;

/// <summary>
/// /api/orders 相关接口的集成测试。
/// 期望值以 docs/requirements.md 为准（而不是以当前实现的运行结果为准）。
///
/// 隔离方式：每个测试创建独立的 WebApplicationFactory，拥有独立的 ShopStore
/// （用户、商品、库存、订单都恢复为初始数据），因此测试之间不依赖执行顺序。
/// Token 一律通过 LoginAs 现场登录获取，不硬编码。
/// </summary>
public class OrderApiIntegrationTests
{
    /// <summary>需求 1.2：演示账号的密码均为 123456。</summary>
    private const string DemoPassword = "123456";

    // ===================== POST /api/orders =====================

    [Fact]
    public async Task PostOrder_NoDiscount_Returns201AndDeductsStock()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = await LoginAs(factory, "alice");
        var stockBefore = await GetStock(client, "p2");

        // 需求 2.2：2.50 × 2 = 5.00，不满 100，不减 ⇒ total = 5.00。
        var response = await client.PostAsJsonAsync("/api/orders",
            new { productId = "p2", quantity = 2 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(order);
        Assert.True(order!.Id > 0);
        Assert.Equal("alice", order.User);
        Assert.Single(order.Items);
        Assert.Equal("p2", order.Items[0].ProductId);
        Assert.Equal("中性笔", order.Items[0].Name);
        Assert.Equal(2.50m, order.Items[0].Price);
        Assert.Equal(2, order.Items[0].Quantity);
        Assert.Equal(5.00m, order.Items[0].Subtotal);
        Assert.Equal(5.00m, order.Total);
        Assert.Equal("学生公寓 3 栋 402", order.Address);
        Assert.Equal("13800000001", order.Phone);

        // 需求 4.5：201 的 Location 指向新订单。
        Assert.Equal($"/api/orders/{order.Id}", response.Headers.Location!.ToString());

        // 需求 4.2：扣减库存。500 − 2 = 498。
        Assert.Equal(stockBefore - 2, await GetStock(client, "p2"));

        var orders = await GetOrders(client);
        Assert.Contains(orders, o => o.Id == order.Id);

        var detailResponse = await client.GetAsync($"/api/orders/{order.Id}");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        var detail = await detailResponse.Content.ReadFromJsonAsync<OrderDto>();
        Assert.Equal(order.Id, detail!.Id);
        Assert.Equal(order.Total, detail.Total);
    }

    [Fact]
    public async Task PostOrder_DiscountBoundaryAt100_Returns201With90()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = await LoginAs(factory, "alice");

        // 需求 2.1：50.00 × 2 = 100.00，金额 ≥ 100 减 10 ⇒ total = 90.00。
        var response = await client.PostAsJsonAsync("/api/orders",
            new { productId = "p1", quantity = 2 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        Assert.Equal(100.00m, order!.Items[0].Subtotal);
        Assert.Equal(90.00m, order.Total);

        // 需求 4.2：100 − 2 = 98。
        Assert.Equal(98, await GetStock(client, "p1"));
    }

    [Fact]
    public async Task PostOrder_JustBelowDiscount_Returns201With97Point50()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = await LoginAs(factory, "alice");

        // 需求 2.2：2.50 × 39 = 97.50，不满 100，不减 ⇒ total = 97.50。
        var response = await client.PostAsJsonAsync("/api/orders",
            new { productId = "p2", quantity = 39 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        Assert.Equal(97.50m, order!.Total);

        // 需求 4.2：500 − 39 = 461。
        Assert.Equal(461, await GetStock(client, "p2"));
    }

    [Fact]
    public async Task PostOrder_ExactStock_Returns201AndStockBecomesZero()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = await LoginAs(factory, "alice");
        Assert.Equal(10, await GetStock(client, "p5"));

        // 需求 2.2：89.90 × 10 = 899.00，不满 100？899 ≥ 100，减 10 ⇒ 889.00。
        // 注意：此处 899.00 已满 100，满减后 total = 889.00。
        var response = await client.PostAsJsonAsync("/api/orders",
            new { productId = "p5", quantity = 10 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        Assert.Equal(889.00m, order!.Total);

        // 需求 4.1/4.2：库存恰好等于数量时允许下单，扣减后为 0。
        Assert.Equal(0, await GetStock(client, "p5"));
    }

    [Fact]
    public async Task PostOrder_MaxQuantity_Returns201WithDiscountedTotal()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = await LoginAs(factory, "alice");

        // 需求 4.6 上边界 99；需求 2.1：50.00 × 99 = 4950.00 ≥ 100 ⇒ 4940.00。
        var response = await client.PostAsJsonAsync("/api/orders",
            new { productId = "p1", quantity = 99 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        Assert.Equal(4940.00m, order!.Total);

        // 需求 4.2：100 − 99 = 1。
        Assert.Equal(1, await GetStock(client, "p1"));
    }

    [Fact]
    public async Task PostOrder_MissingQuantity_Returns400()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = await LoginAs(factory, "alice");
        var stockBefore = await GetStock(client, "p1");
        var ordersBefore = (await GetOrders(client)).Count;

        // quantity 缺省按 0 处理；需求 4.6：数量必须是 1～99。
        var response = await client.PostAsJsonAsync("/api/orders", new { productId = "p1" });

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, "数量必须在 1 到 99 之间");
        Assert.Equal(stockBefore, await GetStock(client, "p1"));
        Assert.Equal(ordersBefore, (await GetOrders(client)).Count);
    }

    [Fact]
    public async Task PostOrder_MissingProductId_Returns404()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = await LoginAs(factory, "alice");
        var ordersBefore = (await GetOrders(client)).Count;

        // 契约未声明 productId 必填；缺少时按“商品不存在”处理（已确认按 404 断言）。
        var response = await client.PostAsJsonAsync("/api/orders", new { quantity = 1 });

        await AssertErrorAsync(response, HttpStatusCode.NotFound, "商品不存在");
        Assert.Equal(ordersBefore, (await GetOrders(client)).Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public async Task PostOrder_QuantityOutside1To99_Returns400(int quantity)
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = await LoginAs(factory, "alice");
        var stockBefore = await GetStock(client, "p1");
        var ordersBefore = (await GetOrders(client)).Count;

        // 需求 4.6：数量必须是 1～99。
        var response = await client.PostAsJsonAsync("/api/orders",
            new { productId = "p1", quantity });

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, "数量必须在 1 到 99 之间");
        Assert.Equal(stockBefore, await GetStock(client, "p1"));
        Assert.Equal(ordersBefore, (await GetOrders(client)).Count);
    }

    [Fact]
    public async Task PostOrder_NegativeQuantity_Returns400()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = await LoginAs(factory, "alice");
        var stockBefore = await GetStock(client, "p1");
        var ordersBefore = (await GetOrders(client)).Count;

        // 需求 4.6：数量必须是 1～99 的整数，-1 非法。
        // 当前实现只拦截 0 和 >99，此用例用于暴露该缺陷（预期失败）。
        var response = await client.PostAsJsonAsync("/api/orders",
            new { productId = "p1", quantity = -1 });

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, "数量必须在 1 到 99 之间");
        Assert.Equal(stockBefore, await GetStock(client, "p1"));
        Assert.Equal(ordersBefore, (await GetOrders(client)).Count);
    }

    [Fact]
    public async Task PostOrder_NonIntegerQuantity_Returns400()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = await LoginAs(factory, "alice");
        var stockBefore = await GetStock(client, "p1");
        var ordersBefore = (await GetOrders(client)).Count;

        // 需求 4.6：数量必须是整数，1.5 无法绑定到 int，请求在进入处理逻辑前被拒。
        var response = await client.PostAsJsonAsync("/api/orders",
            new { productId = "p1", quantity = 1.5 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(stockBefore, await GetStock(client, "p1"));
        Assert.Equal(ordersBefore, (await GetOrders(client)).Count);
    }

    [Fact]
    public async Task PostOrder_UnknownProduct_Returns404()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = await LoginAs(factory, "alice");
        var ordersBefore = (await GetOrders(client)).Count;

        var response = await client.PostAsJsonAsync("/api/orders",
            new { productId = "p999", quantity = 1 });

        await AssertErrorAsync(response, HttpStatusCode.NotFound, "商品不存在");
        Assert.Equal(ordersBefore, (await GetOrders(client)).Count);
    }

    [Fact]
    public async Task PostOrder_WithoutToken_Returns401()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var anonymous = factory.CreateClient();
        var stockBefore = await GetStock(anonymous, "p1");

        var response = await anonymous.PostAsJsonAsync("/api/orders",
            new { productId = "p1", quantity = 1 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsStringAsync());

        // 未认证请求不得产生副作用。
        using var alice = await LoginAs(factory, "alice");
        Assert.Equal(stockBefore, await GetStock(alice, "p1"));
        Assert.Single(await GetOrders(alice)); // 只有初始订单 1001
    }

    [Fact]
    public async Task PostOrder_InvalidToken_Returns401()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

        var response = await client.PostAsJsonAsync("/api/orders",
            new { productId = "p1", quantity = 1 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task PostOrder_InsufficientStock_Returns409WithoutSideEffects()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = await LoginAs(factory, "alice");
        var ordersBefore = (await GetOrders(client)).Count;
        Assert.Equal(30, await GetStock(client, "p3"));

        // 需求 4.1：库存 30 < 请求 31，返回“库存不足”，不得扣款、不得扣库存。
        var response = await client.PostAsJsonAsync("/api/orders",
            new { productId = "p3", quantity = 31 });

        await AssertErrorAsync(response, HttpStatusCode.Conflict, "库存不足");
        Assert.Equal(30, await GetStock(client, "p3"));
        Assert.Equal(ordersBefore, (await GetOrders(client)).Count);
    }

    [Fact]
    public async Task PostOrder_OutOfStock_Returns409()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = await LoginAs(factory, "alice");
        var ordersBefore = (await GetOrders(client)).Count;
        Assert.Equal(0, await GetStock(client, "p6"));

        var response = await client.PostAsJsonAsync("/api/orders",
            new { productId = "p6", quantity = 1 });

        await AssertErrorAsync(response, HttpStatusCode.Conflict, "库存不足");
        Assert.Equal(0, await GetStock(client, "p6"));
        Assert.Equal(ordersBefore, (await GetOrders(client)).Count);
    }

    [Fact]
    public async Task PostOrder_PaymentFails_Returns402AndRestoresStock()
    {
        // 现有商品目录 + 数量上限 99 时，最大应付金额为 p1 × 99 = 4940 元，
        // 无法通过公开 API 触发“> 5000 支付失败”的规则（该阈值已由 OrderServiceTests 覆盖）。
        // 这里用测试专用工厂把支付网关替换为必定失败的替身，
        // 只验证 API 层的 402 响应、错误信息与库存恢复，不修改被测代码。
        using var factory = new PaymentFailureFactory();
        using var client = await LoginAs(factory, "alice");
        var ordersBefore = (await GetOrders(client)).Count;
        Assert.Equal(500, await GetStock(client, "p2"));

        // 需求 4.3：支付失败 → 恢复库存，返回“支付失败”。500 − 2 + 2 = 500。
        var response = await client.PostAsJsonAsync("/api/orders",
            new { productId = "p2", quantity = 2 });

        await AssertErrorAsync(response, HttpStatusCode.PaymentRequired, "支付失败");
        Assert.Equal(500, await GetStock(client, "p2"));
        Assert.Equal(ordersBefore, (await GetOrders(client)).Count);
    }

    // ===================== GET /api/orders =====================

    [Fact]
    public async Task GetOrders_ReturnsOnlyOwnOrders()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = await LoginAs(factory, "alice");

        var orders = await GetOrders(client);

        // 初始数据：alice 只有 1001（50.00 × 2 = 100.00 ≥ 100 ⇒ 90.00），bob 的 1002 不可见。
        Assert.Contains(orders, o => o.Id == 1001);
        Assert.DoesNotContain(orders, o => o.Id == 1002);
        Assert.All(orders, o => Assert.Equal("alice", o.User));
        Assert.Equal(90.00m, orders.Single(o => o.Id == 1001).Total);
    }

    [Fact]
    public async Task GetOrders_NoOrders_ReturnsEmptyArray()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = await LoginAs(factory, "carol");

        var orders = await GetOrders(client);

        Assert.Empty(orders);
    }

    [Fact]
    public async Task GetOrders_AfterPlacingOrder_ContainsNewOrder()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = await LoginAs(factory, "alice");

        // 需求 2.2：2.50 × 2 = 5.00 < 100 ⇒ total = 5.00。
        var created = await client.PostAsJsonAsync("/api/orders",
            new { productId = "p2", quantity = 2 });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var newOrder = await created.Content.ReadFromJsonAsync<OrderDto>();

        var orders = await GetOrders(client);

        var listed = Assert.Single(orders, o => o.Id == newOrder!.Id);
        Assert.Equal(5.00m, listed.Total);
        Assert.Equal("alice", listed.User);
    }

    [Fact]
    public async Task GetOrders_WithoutToken_Returns401()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/orders");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GetOrders_InvalidToken_Returns401()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

        var response = await client.GetAsync("/api/orders");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetOrders_CarolDoesNotSeeOthersOrdersOrPersonalData()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = await LoginAs(factory, "carol");

        var response = await client.GetAsync("/api/orders");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // 需求 4.7：用户只能查看自己的订单，不得泄露他人的地址和电话。
        var text = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("1001", text);
        Assert.DoesNotContain("1002", text);
        Assert.DoesNotContain("学生公寓 3 栋 402", text);
        Assert.DoesNotContain("学生公寓 5 栋 117", text);
        Assert.DoesNotContain("13800000001", text);
        Assert.DoesNotContain("13800000002", text);
    }

    // ===================== GET /api/orders/{id} =====================

    [Fact]
    public async Task GetOrder_OwnHistoricalOrder_Returns200WithFullData()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = await LoginAs(factory, "alice");

        var response = await client.GetAsync("/api/orders/1001");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(order);
        Assert.Equal(1001, order!.Id);
        Assert.Equal("alice", order.User);
        Assert.Single(order.Items);
        // 初始订单明细：50.00 × 2 = 100.00；需求 2.1 满 100 减 10 ⇒ 90.00。
        Assert.Equal(100.00m, order.Items[0].Subtotal);
        Assert.Equal(90.00m, order.Total);
        Assert.Equal("学生公寓 3 栋 402", order.Address);
        Assert.Equal("13800000001", order.Phone);
    }

    [Fact]
    public async Task GetOrder_OwnNewOrder_MatchesCreateResponse()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = await LoginAs(factory, "alice");

        // 需求 2.2：2.50 × 2 = 5.00 < 100 ⇒ total = 5.00。
        var createdResponse = await client.PostAsJsonAsync("/api/orders",
            new { productId = "p2", quantity = 2 });
        var created = await createdResponse.Content.ReadFromJsonAsync<OrderDto>();

        var response = await client.GetAsync($"/api/orders/{created!.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var fetched = await response.Content.ReadFromJsonAsync<OrderDto>();
        Assert.Equal(created.Id, fetched!.Id);
        Assert.Equal(created.User, fetched.User);
        Assert.Equal(created.Total, fetched.Total);
        Assert.Equal(created.Items.Count, fetched.Items.Count);
        Assert.Equal(created.Address, fetched.Address);
        Assert.Equal(created.Phone, fetched.Phone);
    }

    [Fact]
    public async Task GetOrder_NotFound_Returns404()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = await LoginAs(factory, "alice");

        var response = await client.GetAsync("/api/orders/999999");

        await AssertErrorAsync(response, HttpStatusCode.NotFound, "订单不存在");
    }

    [Fact]
    public async Task GetOrder_NonIntegerId_Returns404()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = await LoginAs(factory, "alice");

        // 路由约束 {id:int} 不匹配，直接 404，无业务错误体。
        var response = await client.GetAsync("/api/orders/abc");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetOrder_NegativeId_Returns404()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = await LoginAs(factory, "alice");

        var response = await client.GetAsync("/api/orders/-1");

        await AssertErrorAsync(response, HttpStatusCode.NotFound, "订单不存在");
    }

    [Fact]
    public async Task GetOrder_WithoutToken_Returns401()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/orders/1001");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GetOrder_InvalidToken_Returns401()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

        var response = await client.GetAsync("/api/orders/1001");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetOrder_OtherUsersOrder_Returns403Or404WithoutLeakingPersonalData()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = await LoginAs(factory, "bob");

        // 需求 4.7：bob 查看 alice 的订单 1001，必须返回 403 或 404，
        // 且不得泄露 alice 的地址和电话。
        // 当前实现只校验“是否登录”，会返回 200 并泄露数据；此用例用于暴露该缺陷（预期失败）。
        var response = await client.GetAsync("/api/orders/1001");

        Assert.True(response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound,
            $"期望 403 或 404，实际 {(int)response.StatusCode}");

        var text = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("学生公寓 3 栋 402", text);
        Assert.DoesNotContain("13800000001", text);
    }

    [Fact]
    public async Task GetOrder_AliceRequestsBobsNewOrder_Returns403Or404()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var bob = await LoginAs(factory, "bob");
        using var alice = await LoginAs(factory, "alice");

        // 订单号由测试现场创建获得，不写死。
        // 需求 2.2：2.50 × 2 = 5.00 < 100，不满减 ⇒ total = 5.00。
        var createdResponse = await bob.PostAsJsonAsync("/api/orders",
            new { productId = "p2", quantity = 2 });
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var bobOrder = await createdResponse.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(bobOrder);
        Assert.True(bobOrder!.Id > 0);
        Assert.Equal("bob", bobOrder.User);

        // 先确认这笔订单确实存在且归 bob 所有。
        var bobResponse = await bob.GetAsync($"/api/orders/{bobOrder.Id}");
        Assert.Equal(HttpStatusCode.OK, bobResponse.StatusCode);

        // 需求 4.7：alice 查看 bob 的订单必须返回 403 或 404，且不得泄露 bob 的地址和电话。
        // 当前实现只校验“是否登录”，会返回 200；此用例用于暴露该缺陷（预期失败）。
        var response = await alice.GetAsync($"/api/orders/{bobOrder.Id}");

        Assert.True(response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound,
            $"期望 403 或 404，实际 {(int)response.StatusCode}");

        var text = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("学生公寓 5 栋 117", text);
        Assert.DoesNotContain("13800000002", text);
    }

    // ===================== 辅助方法 =====================

    /// <summary>用演示账号现场登录，返回已带 Bearer Token 的客户端。不得硬编码 Token。</summary>
    private static async Task<HttpClient> LoginAs(WebApplicationFactory<Program> factory, string user)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(user, DemoPassword));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.False(string.IsNullOrWhiteSpace(login?.Token));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login!.Token);
        return client;
    }

    private static async Task<List<OrderDto>> GetOrders(HttpClient client)
    {
        var response = await client.GetAsync("/api/orders");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<List<OrderDto>>())!;
    }

    private static async Task<int> GetStock(HttpClient client, string productId)
    {
        var products = await client.GetFromJsonAsync<List<ProductDto>>("/api/products");
        return products!.Single(p => p.Id == productId).Stock;
    }

    private static async Task AssertErrorAsync(
        HttpResponseMessage response, HttpStatusCode expectedStatus, string expectedMessage)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal(expectedMessage, error!.Error);
    }

    /// <summary>
    /// 测试专用工厂：把 IPaymentGateway 替换为必定失败的替身，
    /// 用于在 API 层验证需求 4.3（支付失败 → 402 + 恢复库存）。
    /// 5000 元阈值本身由 OrderServiceTests 的单元测试覆盖，这里不重复断言。
    /// 只覆盖测试宿主的 DI，不修改被测代码。
    /// </summary>
    private sealed class PaymentFailureFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IPaymentGateway>();
                services.AddSingleton<IPaymentGateway, AlwaysFailingPaymentGateway>();
            });
        }

        private sealed class AlwaysFailingPaymentGateway : IPaymentGateway
        {
            public bool Charge(string userId, decimal amount) => false;
        }
    }
}
