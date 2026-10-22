package campusshop;

import java.math.BigDecimal;

/** 购物车中的一项。 */
public record CartItem(String productId, String name, BigDecimal price, int quantity) {

    public BigDecimal getSubtotal() {
        return price.multiply(BigDecimal.valueOf(quantity));
    }
}
