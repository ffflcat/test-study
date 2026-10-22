namespace CampusShop.Web.Pages;

public class CartModel(AuthService auth, ShopStore store, Pricing pricing, CheckoutService checkout)
    : ShopPageModel(auth)
{
    public Cart Cart => store.GetCart(CurrentUser.Username);
    public decimal Total => pricing.TotalFor(CurrentUser, Cart.Subtotal);
    public OrderRecord? PlacedOrder { get; set; }
    public string? Error { get; set; }

    public void OnGet()
    {
    }

    public void OnPost()
    {
        PlacedOrder = checkout.Checkout(CurrentUser, out var error);
        Error = PlacedOrder is null ? error : null;
    }
}
