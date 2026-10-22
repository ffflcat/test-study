namespace CampusShop.Tests;

/// <summary>
/// CouponService.IsUsable / Apply 的单元测试。
/// 需求见 docs/requirements.md 第 3 节：
/// 未过期（截止日当天仍可用）、金额达到门槛（金额 ≥ 门槛）、未被使用，
/// 三个条件同时满足时优惠券可用；可用时应付 = 订单金额 − 券面额，不可用时金额不变。
/// </summary>
public class CouponServiceTests
{
    private const decimal FaceValue = 10.00m;

    /// <remarks>
    /// 参数依次为：截止日相对今天的天数、订单金额、券门槛、是否已使用、期望可用性。
    /// IsUsable 内部读取 DateTime.Now，无法注入当前日期，因此用相对天数表达
    /// T−1（昨天）/ T（今天）/ T+1（明天），避免测试随时间失效。
    /// 门槛为 0、金额为 0 已确认不属于合法业务输入，未纳入用例。
    /// </remarks>
    public static TheoryData<int, decimal, decimal, bool, bool> IsUsableCases => new()
    {
        { 1, 100.01m, 100.00m, false, true },
        { 1, 100.00m, 100.00m, false, true },
        { 0, 100.00m, 100.00m, false, true },
        { 1, 150.00m, 100.00m, false, true },
        { -1, 100.00m, 100.00m, false, false },
        { -1, 1000.00m, 100.00m, false, false },
        { 1, 99.99m, 100.00m, false, false },
        { 1, 100.00m, 100.00m, true, false },
        { 0, 100.00m, 100.00m, true, false },
        { -1, 50.00m, 100.00m, false, false },
        { 1, 99.99m, 100.00m, true, false },
        { -1, 100.00m, 100.00m, true, false },
        { -1, 50.00m, 100.00m, true, false },
    };

    [Theory]
    [MemberData(nameof(IsUsableCases))]
    public void IsUsable_GivenCouponAndAmount_ReturnsExpected(
        int expiryOffsetDays, decimal amount, decimal threshold, bool isUsed, bool expected)
    {
        var coupon = CreateCoupon(expiryOffsetDays, threshold, FaceValue, isUsed);
        var service = new CouponService();

        var actual = service.IsUsable(coupon, amount);

        Assert.Equal(expected, actual);
    }

    /// <remarks>
    /// 参数依次为：截止日相对今天的天数、订单金额、券门槛、券面额、是否已使用、期望应付金额。
    /// </remarks>
    public static TheoryData<int, decimal, decimal, decimal, bool, decimal> ApplyCases => new()
    {
        { 1, 100.00m, 100.00m, 10.00m, false, 90.00m },
        { 1, 150.00m, 100.00m, 10.00m, false, 140.00m },
        { 0, 100.00m, 100.00m, 10.00m, false, 90.00m },
        { -1, 100.00m, 100.00m, 10.00m, false, 100.00m },
        { 1, 99.99m, 100.00m, 10.00m, false, 99.99m },
        { 1, 100.00m, 100.00m, 10.00m, true, 100.00m },
        { -1, 50.00m, 100.00m, 10.00m, true, 50.00m },
        { 1, 100.00m, 100.00m, 100.00m, false, 0.00m },
    };

    [Theory]
    [MemberData(nameof(ApplyCases))]
    public void Apply_GivenCouponAndAmount_ReturnsExpectedPayable(
        int expiryOffsetDays, decimal amount, decimal threshold, decimal couponAmount,
        bool isUsed, decimal expected)
    {
        var coupon = CreateCoupon(expiryOffsetDays, threshold, couponAmount, isUsed);
        var service = new CouponService();

        var actual = service.Apply(coupon, amount);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IsUsable_NullCoupon_ThrowsNullReferenceException()
    {
        var service = new CouponService();

        Assert.Throws<NullReferenceException>(() => service.IsUsable(null!, 100.00m));
    }

    [Fact]
    public void Apply_NullCoupon_ThrowsNullReferenceException()
    {
        var service = new CouponService();

        Assert.Throws<NullReferenceException>(() => service.Apply(null!, 100.00m));
    }

    private static Coupon CreateCoupon(int expiryOffsetDays, decimal threshold,
        decimal amount, bool isUsed) => new()
    {
        Code = "TEST",
        Threshold = threshold,
        Amount = amount,
        ExpireDate = DateOnly.FromDateTime(DateTime.Now).AddDays(expiryOffsetDays),
        IsUsed = isUsed,
    };
}
