namespace CampusShop;

/// <summary>购物车中的一项。</summary>
public record CartItem(string ProductId, string Name, decimal Price, int Quantity)
{
    public decimal Subtotal => Price * Quantity;
}

/// <summary>
/// 购物车：同一商品多次加入时合并数量。
/// </summary>
public class Cart
{
    private readonly List<CartItem> _items = new();

    public IReadOnlyList<CartItem> Items => _items;

    public bool IsEmpty => _items.Count == 0;

    /// <summary>加入商品；已有该商品时累加数量。</summary>
    public void Add(Product product, int quantity)
    {
        var index = _items.FindIndex(i => i.ProductId == product.Id);
        if (index >= 0)
            _items[index] = _items[index] with { Quantity = _items[index].Quantity + quantity };
        else
            _items.Add(new CartItem(product.Id, product.Name, product.Price, quantity));
    }

    /// <summary>移除商品；商品不在购物车中时什么也不做。</summary>
    public void Remove(string productId) => _items.RemoveAll(i => i.ProductId == productId);

    public void Clear() => _items.Clear();

    /// <summary>商品原价合计（未打折、未满减）。</summary>
    public decimal Subtotal => _items.Sum(i => i.Subtotal);
}
