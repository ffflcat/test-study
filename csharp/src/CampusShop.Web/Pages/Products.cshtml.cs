using Microsoft.AspNetCore.Mvc;

namespace CampusShop.Web.Pages;

public class ProductsModel(AuthService auth, ShopStore store) : ShopPageModel(auth)
{
    public IReadOnlyList<Product> Products => store.Products;
    public string? Message { get; set; }

    public int Stock(string productId) => store.GetStock(productId);

    public void OnGet()
    {
    }

    public IActionResult OnPost(string productId, int quantity)
    {
        if (store.FindProduct(productId) is not { } product)
            return NotFound();
        if (quantity < 1 || quantity > ShopApi.MaxQuantity)
        {
            Message = $"数量必须在 1 到 {ShopApi.MaxQuantity} 之间";
            return Page();
        }
        store.GetCart(CurrentUser.Username).Add(product, quantity);
        Message = $"已将 {quantity} 件{product.Name}加入购物车";
        return Page();
    }
}
