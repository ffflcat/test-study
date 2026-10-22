using CampusShop;

namespace CampusShop.Web;

/// <summary>已生成的订单。</summary>
public record OrderRecord(int Id, string User, List<CartItem> Items, decimal Total,
    string Address, string Phone, DateTime CreatedAt);

/// <summary>
/// 内存中的演示数据：用户、商品、库存、购物车、订单。
/// 每次启动网站时恢复为初始数据。
/// </summary>
public class ShopStore : IInventoryRepository
{
    private readonly object _lock = new();
    private readonly Dictionary<string, int> _stock = new();
    private readonly Dictionary<string, Cart> _carts = new();
    private readonly Dictionary<int, OrderRecord> _orders = new();

    public List<User> Users { get; } = new();
    public List<Product> Products { get; } = new();

    public ShopStore() => Reset();

    /// <summary>恢复为初始数据。</summary>
    public void Reset()
    {
        lock (_lock)
        {
            Users.Clear();
            Users.AddRange(new[]
            {
                new User("alice", "123456", "学生公寓 3 栋 402", "13800000001"),
                new User("bob", "123456", "学生公寓 5 栋 117", "13800000002"),
                new User("carol", "123456", "教师公寓 1 栋 201", "13800000003"),
            });

            Products.Clear();
            Products.AddRange(new[]
            {
                new Product("p1", "笔记本", 50.00m),
                new Product("p2", "中性笔", 2.50m),
                new Product("p3", "保温杯", 39.90m),
                new Product("p4", "双肩包", 129.00m),
                new Product("p5", "台灯", 89.90m),
                new Product("p6", "纪念徽章", 15.00m),
            });

            _stock.Clear();
            _stock["p1"] = 100;
            _stock["p2"] = 500;
            _stock["p3"] = 30;
            _stock["p4"] = 20;
            _stock["p5"] = 10;
            _stock["p6"] = 0;

            _carts.Clear();
            _orders.Clear();
            _orders[1001] = new OrderRecord(1001, "alice",
                new() { new CartItem("p1", "笔记本", 50.00m, 2) }, 90.00m,
                "学生公寓 3 栋 402", "13800000001", new DateTime(2026, 9, 1, 10, 0, 0));
            _orders[1002] = new OrderRecord(1002, "bob",
                new() { new CartItem("p4", "双肩包", 129.00m, 1) }, 119.00m,
                "学生公寓 5 栋 117", "13800000002", new DateTime(2026, 9, 2, 15, 30, 0));
        }
    }

    public Product? FindProduct(string id) => Products.FirstOrDefault(p => p.Id == id);

    public Cart GetCart(string user)
    {
        lock (_lock)
        {
            if (!_carts.TryGetValue(user, out var cart))
                _carts[user] = cart = new Cart();
            return cart;
        }
    }

    public void AddOrder(OrderRecord order)
    {
        lock (_lock) _orders[order.Id] = order;
    }

    public OrderRecord? FindOrder(int id)
    {
        lock (_lock) return _orders.GetValueOrDefault(id);
    }

    public List<OrderRecord> OrdersOf(string user)
    {
        lock (_lock) return _orders.Values.Where(o => o.User == user).OrderBy(o => o.Id).ToList();
    }

    // ---- IInventoryRepository ----
    public int GetStock(string productId)
    {
        lock (_lock) return _stock.GetValueOrDefault(productId);
    }

    public void Deduct(string productId, int quantity)
    {
        lock (_lock) _stock[productId] = _stock.GetValueOrDefault(productId) - quantity;
    }

    public void Restore(string productId, int quantity)
    {
        lock (_lock) _stock[productId] = _stock.GetValueOrDefault(productId) + quantity;
    }
}

/// <summary>模拟支付：单笔金额超过 5000 元时支付失败。</summary>
public class FakePaymentGateway : IPaymentGateway
{
    public const decimal Limit = 5000m;
    public bool Charge(string userId, decimal amount) => amount <= Limit;
}

/// <summary>模拟通知：输出到控制台。</summary>
public class ConsoleNotificationSender : INotificationSender
{
    public void Send(Notification n) => Console.WriteLine($"[通知] {n.UserId}: {n.Message}");
}
