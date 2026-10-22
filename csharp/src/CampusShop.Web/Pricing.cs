using CampusShop;

namespace CampusShop.Web;

/// <summary>计算某个用户的应付金额。</summary>
public class Pricing(PriceCalculator calc)
{
    public decimal TotalFor(User user, decimal subtotal) => calc.ApplyDiscount(subtotal);
}
