namespace CampusShop.Tests;

public class CartTests
{
    private static readonly Product Notebook = new("p1", "笔记本", 50.00m);
    private static readonly Product Pen = new("p2", "中性笔", 2.50m);

    [Fact]
    public void Add_TwoProducts_SubtotalIsSum()
    {
        var cart = new Cart();

        cart.Add(Notebook, 2);
        cart.Add(Pen, 4);

        Assert.Equal(110.00m, cart.Subtotal);
    }

    [Fact]
    public void Add_SameProductTwice_MergesQuantity()
    {
        var cart = new Cart();

        cart.Add(Notebook, 1);
        cart.Add(Notebook, 2);

        var item = Assert.Single(cart.Items);
        Assert.Equal(3, item.Quantity);
    }

    [Fact]
    public void Remove_ExistingProduct_CartBecomesEmpty()
    {
        var cart = new Cart();
        cart.Add(Notebook, 1);

        cart.Remove("p1");

        Assert.True(cart.IsEmpty);
    }
}
