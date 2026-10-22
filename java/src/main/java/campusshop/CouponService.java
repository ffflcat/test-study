package campusshop;

import java.math.BigDecimal;
import java.time.LocalDate;

/**
 * 优惠券规则。见 docs/requirements.md 第 3 节。
 */
public class CouponService {

    /**
     * 判断优惠券能否用于金额为 amount 的订单。
     */
    public boolean isUsable(Coupon c, BigDecimal amount) {
        LocalDate today = LocalDate.now();
        if (!today.isBefore(c.getExpireDate()))
            return false;
        if (amount.compareTo(c.getThreshold()) < 0)
            return false;
        return !c.isUsed();
    }

    /**
     * 使用优惠券后的应付金额；优惠券不可用时返回原金额。
     */
    public BigDecimal apply(Coupon c, BigDecimal amount) {
        if (!isUsable(c, amount))
            return amount;
        return amount.subtract(c.getAmount());
    }
}
