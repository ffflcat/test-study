package campusshop;

import java.math.BigDecimal;

/**
 * 订单计价。规则见 docs/requirements.md 第 2 节。
 */
public class PriceCalculator {

    /**
     * 满减：订单金额满 100 元减 10 元。
     */
    public BigDecimal applyDiscount(BigDecimal total) {
        if (total.compareTo(new BigDecimal("100")) > 0)
            return total.subtract(BigDecimal.TEN);
        return total;
    }
}
