using CampusShop.Web;

namespace CampusShop.Tests;

/// <summary>
/// OrderService.PlaceOrder 的单元测试。
/// 需求见 docs/requirements.md 第 4 节：
/// 4.1 库存不足则返回“库存不足”，不得扣款；4.2 扣减库存；
/// 4.3 支付失败恢复库存并返回“支付失败”；4.4 通知失败不影响订单成功；
/// 4.5 返回“下单成功”及订单号；4.8 单笔金额超过 5000 元时支付失败。
/// 需求 4.6（数量 1～99）由 API 层保证，服务层无校验，未纳入本测试。
/// </summary>
public class OrderServiceTests
{
    private const string UserId = "alice";
    private const string ProductId = "p1";

    /// <remarks>
    /// 参数依次为：购买数量、初始库存、订单金额、通知是否抛异常、预期状态、
    /// 预期提示、预期最终库存、支付是否被调用、通知是否被尝试发送。
    /// 支付结果交给 FakePaymentGateway 按需求 4.8 判定（金额 ≤ 5000 成功），
    /// 替身只记录调用，不在测试里重写这条规则。
    /// 数量 0 / 负数 / 100 及金额 ≤ 0 已确认不属于服务层用例。
    /// </remarks>
    public static TheoryData<int, int, decimal, bool, OrderStatus, string, int, bool, bool> PlaceOrderCases => new()
    {
        // T01 数量下边界，正常下单
        { 1, 10, 50.00m, false, OrderStatus.Success, "下单成功", 9, true, true },
        // T02 数量下边界内侧
        { 2, 10, 90.00m, false, OrderStatus.Success, "下单成功", 8, true, true },
        // T04 数量上边界
        { 99, 100, 4940.00m, false, OrderStatus.Success, "下单成功", 1, true, true },
        // T05 数量上边界内侧
        { 98, 100, 4890.00m, false, OrderStatus.Success, "下单成功", 2, true, true },
        // T07 库存恰好等于数量
        { 10, 10, 500.00m, false, OrderStatus.Success, "下单成功", 0, true, true },
        // T08 库存比数量多 1
        { 10, 11, 500.00m, false, OrderStatus.Success, "下单成功", 1, true, true },
        // T09 库存比数量少 1
        { 10, 9, 500.00m, false, OrderStatus.InsufficientStock, "库存不足", 9, false, false },
        // T10 库存为 0
        { 1, 0, 50.00m, false, OrderStatus.InsufficientStock, "库存不足", 0, false, false },
        // T11 支付金额下侧边界
        { 1, 10, 4999.99m, false, OrderStatus.Success, "下单成功", 9, true, true },
        // T12 支付金额恰好 5000（“超过”才失败）
        { 1, 10, 5000.00m, false, OrderStatus.Success, "下单成功", 9, true, true },
        // T13 支付金额上侧边界，失败后恢复库存
        { 1, 10, 5000.01m, false, OrderStatus.PaymentFailed, "支付失败", 10, true, false },
        // T14 支付失败恢复库存（另一组数值）
        { 3, 5, 6000.00m, false, OrderStatus.PaymentFailed, "支付失败", 5, true, false },
        // T15 支付成功但通知抛异常：订单仍成功
        { 1, 10, 50.00m, true, OrderStatus.Success, "下单成功", 9, true, true },
        // T16 库存不足优先于支付失败
        { 10, 9, 6000.00m, false, OrderStatus.InsufficientStock, "库存不足", 9, false, false },
        // T17 库存不足 + 通知会抛异常：提前返回，通知未发送
        { 10, 9, 500.00m, true, OrderStatus.InsufficientStock, "库存不足", 9, false, false },
        // T18 支付失败 + 通知会抛异常：不发通知，库存恢复
        { 1, 10, 6000.00m, true, OrderStatus.PaymentFailed, "支付失败", 10, true, false },
    };

    [Theory]
    [MemberData(nameof(PlaceOrderCases))]
    public void PlaceOrder_GivenOrderAndStock_ReturnsExpectedResult(
        int quantity, int initialStock, decimal total, bool notificationThrows,
        OrderStatus expectedStatus, string expectedMessage, int expectedFinalStock,
        bool expectPaymentCalled, bool expectNotificationSent)
    {
        var inventory = new StubInventoryRepository(initialStock);
        var payment = new SpyPaymentGateway();
        var notification = new SpyNotificationSender(notificationThrows);
        var service = new OrderService(payment, inventory, notification);

        var result = service.PlaceOrder(new Order(UserId, ProductId, quantity, total));

        Assert.Equal(expectedStatus, result.Status);
        Assert.Equal(expectedMessage, result.Message);
        Assert.Equal(expectedFinalStock, inventory.GetStock(ProductId));
        Assert.Equal(expectPaymentCalled ? 1 : 0, payment.Calls.Count);
        Assert.Equal(expectNotificationSent ? 1 : 0, notification.Attempts.Count);

        if (expectPaymentCalled)
        {
            Assert.Equal(UserId, payment.Calls[0].UserId);
            Assert.Equal(total, payment.Calls[0].Amount);
        }

        if (expectNotificationSent)
            Assert.Equal(UserId, notification.Attempts[0].UserId);

        if (expectedStatus == OrderStatus.Success)
        {
            Assert.False(string.IsNullOrEmpty(result.OrderId));
            Assert.Matches("^[0-9]+$", result.OrderId!);
        }
    }

    /// <summary>需求 4.5：成功时返回订单号，且订单号为纯数字。</summary>
    [Fact]
    public void PlaceOrder_FirstOrder_ReturnsNumericOrderId()
    {
        var service = CreateService(initialStock: 10);

        var result = service.PlaceOrder(new Order(UserId, ProductId, 1, 50.00m));

        Assert.Equal(OrderStatus.Success, result.Status);
        Assert.False(string.IsNullOrEmpty(result.OrderId));
        Assert.Matches("^[0-9]+$", result.OrderId!);
    }

    /// <summary>需求 4.5：连续下单应返回互不相同且递增的订单号。</summary>
    [Fact]
    public void PlaceOrder_TwoSuccessfulOrders_ReturnsDistinctIncreasingOrderIds()
    {
        var service = CreateService(initialStock: 10);

        var first = service.PlaceOrder(new Order(UserId, ProductId, 1, 50.00m));
        var second = service.PlaceOrder(new Order(UserId, ProductId, 1, 50.00m));

        Assert.Equal(OrderStatus.Success, first.Status);
        Assert.Equal(OrderStatus.Success, second.Status);
        Assert.NotEqual(first.OrderId, second.OrderId);
        Assert.True(long.Parse(second.OrderId!) > long.Parse(first.OrderId!));
    }

    /// <summary>
    /// 唯一的可断言非法输入：订单对象为 null 时解引用抛 NullReferenceException。
    /// 数量、金额的合法性由 API 层负责，服务层不会抛异常。
    /// </summary>
    [Fact]
    public void PlaceOrder_NullOrder_ThrowsNullReferenceException()
    {
        var service = CreateService(initialStock: 10);

        Assert.Throws<NullReferenceException>(() => service.PlaceOrder(null!));
    }

    private static OrderService CreateService(int initialStock) =>
        new(new SpyPaymentGateway(), new StubInventoryRepository(initialStock),
            new SpyNotificationSender(throwOnSend: false));

    /// <summary>可设初始库存的仓储替身，库存读写都体现在同一个字典里。</summary>
    private sealed class StubInventoryRepository : IInventoryRepository
    {
        private readonly Dictionary<string, int> _stock = new();

        public StubInventoryRepository(int initialStock) => _stock[ProductId] = initialStock;

        public int GetStock(string productId) => _stock.GetValueOrDefault(productId);

        public void Deduct(string productId, int quantity) =>
            _stock[productId] = _stock.GetValueOrDefault(productId) - quantity;

        public void Restore(string productId, int quantity) =>
            _stock[productId] = _stock.GetValueOrDefault(productId) + quantity;
    }

    /// <summary>
    /// 支付替身：记录每次调用，判定委托给服务端使用的 FakePaymentGateway，
    /// 从而覆盖需求 4.8 的 5000 元边界。
    /// </summary>
    private sealed class SpyPaymentGateway : IPaymentGateway
    {
        private readonly FakePaymentGateway _inner = new();

        public List<(string UserId, decimal Amount)> Calls { get; } = new();

        public bool Charge(string userId, decimal amount)
        {
            Calls.Add((userId, amount));
            return _inner.Charge(userId, amount);
        }
    }

    /// <summary>通知替身：记录发送尝试，可按需抛异常以验证需求 4.4。</summary>
    private sealed class SpyNotificationSender(bool throwOnSend) : INotificationSender
    {
        public List<Notification> Attempts { get; } = new();

        public void Send(Notification notification)
        {
            Attempts.Add(notification);
            if (throwOnSend)
                throw new InvalidOperationException("模拟通知服务不可用");
        }
    }
}
