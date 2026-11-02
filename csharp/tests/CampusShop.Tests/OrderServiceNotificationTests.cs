using System.Globalization;
using System.Text.RegularExpressions;
using Moq;

namespace CampusShop.Tests;

/// <summary>
/// 针对 PlaceOrder 通知内容（需求 4.4）的测试，用 Moq 捕获真正传给 INotificationSender 的对象。
/// 只校验通知携带的业务信息（UserId、订单号、金额），不比对完整句子，
/// 这样文案微调不会让测试失效，同时能杀死通知字符串上的变异。
/// </summary>
public class OrderServiceNotificationTests
{
    private const string UserId = "alice";
    private const string ProductId = "p1";

    /// <summary>
    /// 支付成功时必须发送一条通知，且通知里带着本次下单的 UserId、订单号和金额。
    /// 金额用 1234.5m（数值上等于 1234.50，但 decimal 默认格式是 "1234.5"）：
    /// 只有按 "0.00" 格式化才会得到两位小数，格式说明符被改坏时这里就会失败。
    /// </summary>
    [Fact]
    public void PlaceOrder_PaymentSucceeds_SendsNotificationWithUserOrderIdAndAmount()
    {
        const decimal total = 1234.5m;

        var inventory = new Mock<IInventoryRepository>();
        inventory.Setup(i => i.GetStock(ProductId)).Returns(10);

        var payment = new Mock<IPaymentGateway>();
        payment.Setup(p => p.Charge(UserId, total)).Returns(true);

        Notification? sent = null;
        var notify = new Mock<INotificationSender>();
        notify.Setup(n => n.Send(It.IsAny<Notification>()))
            .Callback<Notification>(n => sent = n);

        var service = new OrderService(payment.Object, inventory.Object, notify.Object);

        var result = service.PlaceOrder(new Order(UserId, ProductId, 1, total));

        Assert.Equal(OrderStatus.Success, result.Status);
        Assert.NotNull(sent);

        // 通知对象本身指向本次下单的用户
        Assert.Equal(UserId, sent!.UserId);

        // 通知正文里必须带上本次订单号
        Assert.Contains(result.OrderId!, sent.Message);

        // 通知正文里的金额必须等于订单金额，且保留两位小数
        var amount = Regex.Match(sent.Message, @"\d+\.\d{2}");
        Assert.True(amount.Success, $"通知中没有两位小数格式的金额：{sent.Message}");
        Assert.Equal(total, decimal.Parse(amount.Value, CultureInfo.InvariantCulture));

        notify.Verify(n => n.Send(It.IsAny<Notification>()), Times.Once);
        payment.Verify(p => p.Charge(UserId, total), Times.Once);
        inventory.Verify(i => i.Deduct(ProductId, 1), Times.Once);
    }

    /// <summary>
    /// 需求 4.4：通知失败不影响订单成功。
    /// 只断言异常不向上抛出、订单仍然成功并返回订单号；日志内容不校验。
    /// </summary>
    [Fact]
    public void PlaceOrder_NotificationThrows_OrderStillSucceeds()
    {
        var inventory = new Mock<IInventoryRepository>();
        inventory.Setup(i => i.GetStock(ProductId)).Returns(10);

        var payment = new Mock<IPaymentGateway>();
        payment.Setup(p => p.Charge(UserId, It.IsAny<decimal>())).Returns(true);

        var notify = new Mock<INotificationSender>();
        notify.Setup(n => n.Send(It.IsAny<Notification>()))
            .Throws(new InvalidOperationException("模拟通知服务不可用"));

        var service = new OrderService(payment.Object, inventory.Object, notify.Object);

        var result = service.PlaceOrder(new Order(UserId, ProductId, 1, 50.00m));

        Assert.Equal(OrderStatus.Success, result.Status);
        Assert.False(string.IsNullOrEmpty(result.OrderId));
        Assert.Matches("^[0-9]+$", result.OrderId!);

        // 通知确实被尝试发送过，只是异常被吞掉了
        notify.Verify(n => n.Send(It.IsAny<Notification>()), Times.Once);
    }
}
