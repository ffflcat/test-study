package campusshop;

import java.math.BigDecimal;
import java.time.LocalDate;

/**
 * 满减优惠券：订单金额达到 threshold 时减 amount 元，有效期到 expireDate 当天（含）。
 */
public class Coupon {

    private String code = "";
    private BigDecimal threshold = BigDecimal.ZERO;
    private BigDecimal amount = BigDecimal.ZERO;
    private LocalDate expireDate;
    private boolean used;

    public Coupon() {
    }

    public Coupon(String code, BigDecimal threshold, BigDecimal amount, LocalDate expireDate, boolean used) {
        this.code = code;
        this.threshold = threshold;
        this.amount = amount;
        this.expireDate = expireDate;
        this.used = used;
    }

    public String getCode() { return code; }
    public void setCode(String code) { this.code = code; }
    public BigDecimal getThreshold() { return threshold; }
    public void setThreshold(BigDecimal threshold) { this.threshold = threshold; }
    public BigDecimal getAmount() { return amount; }
    public void setAmount(BigDecimal amount) { this.amount = amount; }
    public LocalDate getExpireDate() { return expireDate; }
    public void setExpireDate(LocalDate expireDate) { this.expireDate = expireDate; }
    public boolean isUsed() { return used; }
    public void setUsed(boolean used) { this.used = used; }
}
