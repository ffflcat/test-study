package campusshop;

import java.math.BigDecimal;

/** 支付网关：扣款成功返回 true。 */
public interface PaymentGateway {
    boolean charge(String userId, BigDecimal amount);
}
