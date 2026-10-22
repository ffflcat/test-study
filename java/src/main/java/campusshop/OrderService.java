package campusshop;

import java.util.concurrent.atomic.AtomicInteger;

/**
 * 下单流程。见 docs/requirements.md 第 4 节。
 */
public class OrderService {

    private static final AtomicInteger NEXT_ORDER_ID = new AtomicInteger(1002);

    private final PaymentGateway pay;
    private final InventoryRepository inv;
    private final NotificationSender notify;

    public OrderService(PaymentGateway pay, InventoryRepository inv, NotificationSender notify) {
        this.pay = pay;
        this.inv = inv;
        this.notify = notify;
    }

    public OrderResult placeOrder(Order o) {
        if (inv.getStock(o.productId()) < o.quantity())
            return new OrderResult(OrderStatus.INSUFFICIENT_STOCK, null, "库存不足");

        inv.deduct(o.productId(), o.quantity());

        if (!pay.charge(o.userId(), o.total()))
            return new OrderResult(OrderStatus.PAYMENT_FAILED, null, "支付失败");

        String orderId = String.valueOf(NEXT_ORDER_ID.incrementAndGet());
        try {
            notify.send(new Notification(o.userId(), "订单 " + orderId + " 下单成功，金额 " + o.total() + " 元"));
        } catch (RuntimeException ex) {
            System.err.println("通知发送失败（订单 " + orderId + "）：" + ex.getMessage());
        }
        return new OrderResult(OrderStatus.SUCCESS, orderId, "下单成功");
    }
}
