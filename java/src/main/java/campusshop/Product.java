package campusshop;

import java.math.BigDecimal;

/** 商品。 */
public record Product(String id, String name, BigDecimal price) {
}
