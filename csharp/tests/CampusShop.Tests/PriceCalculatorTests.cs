namespace CampusShop.Tests;

/// <summary>
/// PriceCalculator.ApplyDiscount 的单元测试。
/// 期望值依据 docs/requirements.md 第 2 节：
/// 订单金额满 100 元（≥ 100）减 10 元，不满 100 元不减。
/// </summary>
public class PriceCalculatorTests
{
    /// <remarks>
    /// 使用 MemberData 而不是 InlineData：decimal 不是合法的特性参数类型，
    /// 用 double 字面量再转换可能引入精度问题，与需求 1.4 的精确金额要求冲突。
    /// 负数、超过 2 位小数等非法输入按约定标记为“不测”。
    /// </remarks>
    public static TheoryData<decimal, decimal> DiscountCases => new()
    {
        { 0.00m, 0.00m },
        { 0.01m, 0.01m },
        { 50.00m, 50.00m },
        { 99.99m, 99.99m },
        { 100.00m, 90.00m },
        { 100.01m, 90.01m },
        { 120.00m, 110.00m },
        { 150.00m, 140.00m },
        { 180.00m, 170.00m },
        { 5000.00m, 4990.00m },
    };

    [Theory]
    [MemberData(nameof(DiscountCases))]
    public void ApplyDiscount_GivenTotal_ReturnsRequirementAmount(decimal total, decimal expected)
    {
        var calc = new PriceCalculator();

        var actual = calc.ApplyDiscount(total);

        Assert.Equal(expected, actual);
    }
}
