package campusshop.web;

import campusshop.Cart;
import campusshop.CartItem;
import campusshop.Notification;
import campusshop.User;
import org.springframework.stereotype.Component;

import java.math.BigDecimal;
import java.time.LocalDateTime;
import java.util.ArrayList;
import java.util.concurrent.atomic.AtomicInteger;

/**
 * 网页购物车结算：整单计价，逐个商品扣减库存，一次扣款。
 */
@Component
public class CheckoutService {

    /** 结算结果：成功时 order 不为 null，失败时 error 为原因。 */
    public record Result(OrderRecord order, String error) {
    }

    private static final AtomicInteger NEXT_ORDER_ID = new AtomicInteger(5001);

    private final ShopStore store;
    private final Pricing pricing;
    private final FakePaymentGateway pay;
    private final ConsoleNotificationSender notify;

    public CheckoutService(ShopStore store, Pricing pricing, FakePaymentGateway pay, ConsoleNotificationSender notify) {
        this.store = store;
        this.pricing = pricing;
        this.pay = pay;
        this.notify = notify;
    }

    public Result checkout(User user) {
        Cart cart = store.getCart(user.username());
        if (cart.isEmpty())
            return new Result(null, "购物车为空，无法下单");
        for (CartItem item : cart.getItems()) {
            if (store.getStock(item.productId()) < item.quantity())
                return new Result(null, item.name() + " 库存不足");
        }

        BigDecimal total = pricing.totalFor(user, cart.getSubtotal());
        cart.getItems().forEach(i -> store.deduct(i.productId(), i.quantity()));
        if (!pay.charge(user.username(), total)) {
            cart.getItems().forEach(i -> store.restore(i.productId(), i.quantity()));
            return new Result(null, "支付失败");
        }

        OrderRecord order = new OrderRecord(NEXT_ORDER_ID.incrementAndGet(), user.username(),
                new ArrayList<>(cart.getItems()), total, user.address(), user.phone(), LocalDateTime.now());
        store.addOrder(order);
        cart.clear();
        try {
            notify.send(new Notification(user.username(), "订单 " + order.id() + " 下单成功"));
        } catch (RuntimeException ex) {
            System.err.println("通知发送失败：" + ex.getMessage());
        }
        return new Result(order, null);
    }
}
