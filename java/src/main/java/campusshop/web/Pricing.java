package campusshop.web;

import campusshop.PriceCalculator;
import campusshop.User;
import org.springframework.stereotype.Component;

import java.math.BigDecimal;

/** 计算某个用户的应付金额。 */
@Component
public class Pricing {

    private final PriceCalculator calc;

    public Pricing(PriceCalculator calc) {
        this.calc = calc;
    }

    public BigDecimal totalFor(User user, BigDecimal subtotal) {
        return calc.applyDiscount(subtotal);
    }
}
