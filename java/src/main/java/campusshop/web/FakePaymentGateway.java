package campusshop.web;

import campusshop.PaymentGateway;
import org.springframework.stereotype.Component;

import java.math.BigDecimal;

/** 模拟支付：单笔金额超过 5000 元时支付失败。 */
@Component
public class FakePaymentGateway implements PaymentGateway {

    public static final BigDecimal LIMIT = new BigDecimal("5000");

    @Override
    public boolean charge(String userId, BigDecimal amount) {
        return amount.compareTo(LIMIT) <= 0;
    }
}
