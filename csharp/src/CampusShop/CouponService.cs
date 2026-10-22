namespace CampusShop;

/// <summary>
/// 优惠券规则。见 docs/requirements.md 第 3 节。
/// </summary>
public class CouponService
{
    /// <summary>
    /// 判断优惠券能否用于金额为 amount 的订单。
    /// </summary>
    public bool IsUsable(Coupon c, decimal amount)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        if (today > c.ExpireDate)
            return false;
        if (amount < c.Threshold)
            return false;
        return !c.IsUsed;
    }

    /// <summary>
    /// 使用优惠券后的应付金额；优惠券不可用时返回原金额。
    /// </summary>
    public decimal Apply(Coupon c, decimal amount)
    {
        if (!IsUsable(c, amount))
            return amount;
        return amount - c.Amount;
    }
}
