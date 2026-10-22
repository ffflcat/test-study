namespace CampusShop.Web.Pages;

public class OrdersModel(AuthService auth, ShopStore store) : ShopPageModel(auth)
{
    public List<OrderRecord> Orders { get; private set; } = new();

    public void OnGet() => Orders = store.OrdersOf(CurrentUser.Username);
}
