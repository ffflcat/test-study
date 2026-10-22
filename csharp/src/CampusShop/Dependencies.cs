namespace CampusShop;

/// <summary>支付网关：扣款成功返回 true。</summary>
public interface IPaymentGateway
{
    bool Charge(string userId, decimal amount);
}

/// <summary>库存仓库。</summary>
public interface IInventoryRepository
{
    int GetStock(string productId);
    void Deduct(string productId, int quantity);
    void Restore(string productId, int quantity);
}

/// <summary>通知发送（短信、邮件等）。</summary>
public interface INotificationSender
{
    void Send(Notification notification);
}
