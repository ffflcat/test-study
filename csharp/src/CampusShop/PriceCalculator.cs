namespace CampusShop;

/// <summary>
/// 订单计价。规则见 docs/requirements.md 第 2 节。
/// </summary>
public class PriceCalculator
{
    /// <summary>
    /// 满减：订单金额满 100 元减 10 元。
    /// </summary>
    public decimal ApplyDiscount(decimal total)
    {
        if (total > 100)
            return total - 10;
        return total;
    }
}
