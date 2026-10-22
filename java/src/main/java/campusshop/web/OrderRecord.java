package campusshop.web;

import campusshop.CartItem;

import java.math.BigDecimal;
import java.time.LocalDateTime;
import java.util.List;

/** 已生成的订单。 */
public record OrderRecord(int id, String user, List<CartItem> items, BigDecimal total,
                          String address, String phone, LocalDateTime createdAt) {
}
