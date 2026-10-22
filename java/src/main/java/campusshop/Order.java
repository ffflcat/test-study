package campusshop;

import java.math.BigDecimal;

/** 下单请求：用户、商品、数量、应付金额。 */
public record Order(String userId, String productId, int quantity, BigDecimal total) {
}
