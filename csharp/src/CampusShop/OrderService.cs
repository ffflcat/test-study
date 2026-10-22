namespace CampusShop;

/// <summary>
/// 下单流程。见 docs/requirements.md 第 4 节。
/// </summary>
public class OrderService
{
    private static int _nextOrderId = 1002;

    private readonly IPaymentGateway _pay;
    private readonly IInventoryRepository _inv;
    private readonly INotificationSender _notify;

    public OrderService(IPaymentGateway pay,
        IInventoryRepository inv,
        INotificationSender notify)
    { _pay = pay; _inv = inv; _notify = notify; }

    public OrderResult PlaceOrder(Order o)
    {
        if (_inv.GetStock(o.ProductId) < o.Quantity)
            return new OrderResult(OrderStatus.InsufficientStock, Message: "库存不足");

        _inv.Deduct(o.ProductId, o.Quantity);

        if (!_pay.Charge(o.UserId, o.Total))
            return new OrderResult(OrderStatus.PaymentFailed, Message: "支付失败");

        var orderId = Interlocked.Increment(ref _nextOrderId).ToString();
        try
        {
            _notify.Send(new Notification(o.UserId, $"订单 {orderId} 下单成功，金额 {o.Total:0.00} 元"));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"通知发送失败（订单 {orderId}）：{ex.Message}");
        }
        return new OrderResult(OrderStatus.Success, orderId, "下单成功");
    }
}
