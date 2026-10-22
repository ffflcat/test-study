package campusshop;

/** 下单结果。成功时 orderId 为订单号。 */
public class OrderResult {

    private final OrderStatus status;
    private final String orderId;
    private final String message;

    public OrderResult(OrderStatus status, String orderId, String message) {
        this.status = status;
        this.orderId = orderId;
        this.message = message;
    }

    public OrderStatus getStatus() { return status; }
    public String getOrderId() { return orderId; }
    public String getMessage() { return message; }
}
