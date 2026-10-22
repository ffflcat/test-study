package campusshop.web;

import campusshop.AuthService;
import campusshop.CartItem;
import campusshop.Order;
import campusshop.OrderResult;
import campusshop.OrderService;
import campusshop.OrderStatus;
import campusshop.Product;
import campusshop.User;
import campusshop.web.ApiDtos.CartDto;
import campusshop.web.ApiDtos.CartItemRequest;
import campusshop.web.ApiDtos.CreateOrderRequest;
import campusshop.web.ApiDtos.ErrorResponse;
import campusshop.web.ApiDtos.LoginRequest;
import campusshop.web.ApiDtos.LoginResponse;
import campusshop.web.ApiDtos.OrderDto;
import campusshop.web.ApiDtos.ProductDto;
import io.swagger.v3.oas.annotations.Operation;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import java.math.BigDecimal;
import java.net.URI;
import java.time.LocalDateTime;
import java.util.List;

/**
 * REST API。除登录和商品列表外，都需要请求头 Authorization: Bearer &lt;token&gt;。
 * 接口说明见 /swagger-ui.html。
 */
@RestController
@RequestMapping("/api")
public class ShopApiController {

    public static final int MAX_QUANTITY = 99;

    private final ShopStore store;
    private final AuthService auth;
    private final Pricing pricing;
    private final OrderService orders;

    public ShopApiController(ShopStore store, AuthService auth, Pricing pricing, OrderService orders) {
        this.store = store;
        this.auth = auth;
        this.pricing = pricing;
        this.orders = orders;
    }

    @Operation(summary = "登录，返回 Token")
    @PostMapping("/auth/login")
    public ResponseEntity<?> login(@RequestBody LoginRequest req) {
        String token = auth.login(req.username(), req.password());
        if (token == null)
            return ResponseEntity.status(HttpStatus.UNAUTHORIZED).body(new ErrorResponse("用户名或密码错误"));
        return ResponseEntity.ok(new LoginResponse(token, AuthService.TOKEN_LIFETIME_SECONDS));
    }

    @Operation(summary = "商品列表")
    @GetMapping("/products")
    public List<ProductDto> products() {
        return store.getProducts().stream()
                .map(p -> new ProductDto(p.id(), p.name(), p.price(), store.getStock(p.id())))
                .toList();
    }

    @Operation(summary = "查看购物车")
    @GetMapping("/cart")
    public ResponseEntity<?> cart(@RequestHeader(value = "Authorization", required = false) String authorization) {
        User user = currentUser(authorization);
        if (user == null) return unauthorized();
        var cart = store.getCart(user.username());
        return ResponseEntity.ok(new CartDto(List.copyOf(cart.getItems()), cart.getSubtotal(),
                pricing.totalFor(user, cart.getSubtotal())));
    }

    @Operation(summary = "加入购物车")
    @PostMapping("/cart/items")
    public ResponseEntity<?> addToCart(@RequestBody CartItemRequest req,
                                       @RequestHeader(value = "Authorization", required = false) String authorization) {
        User user = currentUser(authorization);
        if (user == null) return unauthorized();
        if (req.quantity() <= 0 || req.quantity() > MAX_QUANTITY)
            return ResponseEntity.badRequest().body(new ErrorResponse("数量必须在 1 到 " + MAX_QUANTITY + " 之间"));
        Product product = store.findProduct(req.productId());
        if (product == null)
            return ResponseEntity.status(HttpStatus.NOT_FOUND).body(new ErrorResponse("商品不存在"));
        store.getCart(user.username()).add(product, req.quantity());
        return ResponseEntity.noContent().build();
    }

    @Operation(summary = "从购物车移除商品")
    @DeleteMapping("/cart/items/{productId}")
    public ResponseEntity<?> removeFromCart(@PathVariable String productId,
                                            @RequestHeader(value = "Authorization", required = false) String authorization) {
        User user = currentUser(authorization);
        if (user == null) return unauthorized();
        store.getCart(user.username()).remove(productId);
        return ResponseEntity.noContent().build();
    }

    @Operation(summary = "直接下单（单个商品）")
    @PostMapping("/orders")
    public ResponseEntity<?> createOrder(@RequestBody CreateOrderRequest req,
                                         @RequestHeader(value = "Authorization", required = false) String authorization) {
        User user = currentUser(authorization);
        if (user == null) return unauthorized();
        if (req.quantity() == 0 || req.quantity() > MAX_QUANTITY)
            return ResponseEntity.badRequest().body(new ErrorResponse("数量必须在 1 到 " + MAX_QUANTITY + " 之间"));
        Product product = store.findProduct(req.productId());
        if (product == null)
            return ResponseEntity.status(HttpStatus.NOT_FOUND).body(new ErrorResponse("商品不存在"));

        BigDecimal subtotal = product.price().multiply(BigDecimal.valueOf(req.quantity()));
        BigDecimal total = pricing.totalFor(user, subtotal);
        OrderResult result = orders.placeOrder(new Order(user.username(), product.id(), req.quantity(), total));
        if (result.getStatus() == OrderStatus.INSUFFICIENT_STOCK)
            return ResponseEntity.status(HttpStatus.CONFLICT).body(new ErrorResponse(result.getMessage()));
        if (result.getStatus() == OrderStatus.PAYMENT_FAILED)
            return ResponseEntity.status(HttpStatus.PAYMENT_REQUIRED).body(new ErrorResponse(result.getMessage()));

        OrderRecord record = new OrderRecord(Integer.parseInt(result.getOrderId()), user.username(),
                List.of(new CartItem(product.id(), product.name(), product.price(), req.quantity())),
                total, user.address(), user.phone(), LocalDateTime.now());
        store.addOrder(record);
        return ResponseEntity.created(URI.create("/api/orders/" + record.id())).body(OrderDto.of(record));
    }

    @Operation(summary = "我的订单")
    @GetMapping("/orders")
    public ResponseEntity<?> myOrders(@RequestHeader(value = "Authorization", required = false) String authorization) {
        User user = currentUser(authorization);
        if (user == null) return unauthorized();
        return ResponseEntity.ok(store.ordersOf(user.username()).stream().map(OrderDto::of).toList());
    }

    @Operation(summary = "查看订单详情")
    @GetMapping("/orders/{id}")
    public ResponseEntity<?> order(@PathVariable int id,
                                   @RequestHeader(value = "Authorization", required = false) String authorization) {
        if (currentUser(authorization) == null) return unauthorized();
        OrderRecord order = store.findOrder(id);
        if (order == null)
            return ResponseEntity.status(HttpStatus.NOT_FOUND).body(new ErrorResponse("订单不存在"));
        return ResponseEntity.ok(OrderDto.of(order));
    }

    private static ResponseEntity<?> unauthorized() {
        return ResponseEntity.status(HttpStatus.UNAUTHORIZED).build();
    }

    /** 从请求头 Authorization: Bearer &lt;token&gt; 中取出当前用户。 */
    private User currentUser(String authorization) {
        if (authorization == null || !authorization.regionMatches(true, 0, "Bearer ", 0, 7))
            return null;
        String name = auth.validate(authorization.substring(7).trim());
        return name == null ? null : auth.findUser(name);
    }
}
