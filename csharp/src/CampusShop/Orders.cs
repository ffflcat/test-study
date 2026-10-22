namespace CampusShop;

/// <summary>下单请求：用户、商品、数量、应付金额。</summary>
public record Order(string UserId, string ProductId, int Quantity, decimal Total);

public enum OrderStatus
{
    Success,
    InsufficientStock,
    PaymentFailed,
}

/// <summary>下单结果。成功时 OrderId 为订单号。</summary>
public record OrderResult(OrderStatus Status, string? OrderId = null, string Message = "");

/// <summary>发给用户的通知。</summary>
public record Notification(string UserId, string Message);
