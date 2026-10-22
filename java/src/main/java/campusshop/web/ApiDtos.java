package campusshop.web;

import campusshop.CartItem;

import java.math.BigDecimal;
import java.time.LocalDateTime;
import java.util.List;

/** REST API 的请求与响应。 */
public final class ApiDtos {

    private ApiDtos() {
    }

    public record LoginRequest(String username, String password) {
    }

    public record LoginResponse(String token, int expiresIn) {
    }

    public record ErrorResponse(String error) {
    }

    public record ProductDto(String id, String name, BigDecimal price, int stock) {
    }

    public record CartItemRequest(String productId, int quantity) {
    }

    public record CartDto(List<CartItem> items, BigDecimal subtotal, BigDecimal total) {
    }

    public record CreateOrderRequest(String productId, int quantity) {
    }

    public record OrderDto(int id, String user, List<CartItem> items, BigDecimal total,
                           String address, String phone, LocalDateTime createdAt) {

        static OrderDto of(OrderRecord o) {
            return new OrderDto(o.id(), o.user(), o.items(), o.total(), o.address(), o.phone(), o.createdAt());
        }
    }
}
