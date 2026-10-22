namespace CampusShop;

/// <summary>
/// 满减优惠券：订单金额达到 Threshold 时减 Amount 元，有效期到 ExpireDate 当天（含）。
/// </summary>
public class Coupon
{
    public string Code { get; set; } = "";
    public decimal Threshold { get; set; }
    public decimal Amount { get; set; }
    public DateOnly ExpireDate { get; set; }
    public bool IsUsed { get; set; }
}
