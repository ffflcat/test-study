using CampusShop;

namespace CampusShop.Web;

/// <summary>
/// 网页购物车结算：整单计价，逐个商品扣减库存，一次扣款。
/// </summary>
public class CheckoutService(ShopStore store, Pricing pricing, IPaymentGateway pay, INotificationSender notify)
{
    private static int _nextOrderId = 5001;

    /// <summary>结算成功返回订单，失败返回 null，并通过 error 给出原因。</summary>
    public OrderRecord? Checkout(User user, out string error)
    {
        var cart = store.GetCart(user.Username);
        if (cart.IsEmpty)
        {
            error = "购物车为空，无法下单";
            return null;
        }
        foreach (var item in cart.Items)
        {
            if (store.GetStock(item.ProductId) < item.Quantity)
            {
                error = $"{item.Name} 库存不足";
                return null;
            }
        }

        var total = pricing.TotalFor(user, cart.Subtotal);
        foreach (var item in cart.Items)
            store.Deduct(item.ProductId, item.Quantity);
        if (!pay.Charge(user.Username, total))
        {
            foreach (var item in cart.Items)
                store.Restore(item.ProductId, item.Quantity);
            error = "支付失败";
            return null;
        }

        var order = new OrderRecord(Interlocked.Increment(ref _nextOrderId), user.Username,
            cart.Items.ToList(), total, user.Address, user.Phone, DateTime.Now);
        store.AddOrder(order);
        cart.Clear();
        try
        {
            notify.Send(new Notification(user.Username, $"订单 {order.Id} 下单成功"));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"通知发送失败：{ex.Message}");
        }
        error = "";
        return order;
    }
}
