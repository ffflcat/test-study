package campusshop.web;

import campusshop.Cart;
import campusshop.CartItem;
import campusshop.InventoryRepository;
import campusshop.Product;
import campusshop.User;
import org.springframework.stereotype.Component;

import java.math.BigDecimal;
import java.time.LocalDateTime;
import java.util.ArrayList;
import java.util.Comparator;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

/**
 * 内存中的演示数据：用户、商品、库存、购物车、订单。
 * 每次启动网站时恢复为初始数据。
 */
@Component
public class ShopStore implements InventoryRepository {

    private final List<User> users = new ArrayList<>();
    private final List<Product> products = new ArrayList<>();
    private final Map<String, Integer> stock = new HashMap<>();
    private final Map<String, Cart> carts = new HashMap<>();
    private final Map<Integer, OrderRecord> orders = new HashMap<>();

    public ShopStore() {
        reset();
    }

    /** 恢复为初始数据。 */
    public synchronized void reset() {
        users.clear();
        users.add(new User("alice", "123456", "学生公寓 3 栋 402", "13800000001"));
        users.add(new User("bob", "123456", "学生公寓 5 栋 117", "13800000002"));
        users.add(new User("carol", "123456", "教师公寓 1 栋 201", "13800000003"));

        products.clear();
        products.add(new Product("p1", "笔记本", new BigDecimal("50.00")));
        products.add(new Product("p2", "中性笔", new BigDecimal("2.50")));
        products.add(new Product("p3", "保温杯", new BigDecimal("39.90")));
        products.add(new Product("p4", "双肩包", new BigDecimal("129.00")));
        products.add(new Product("p5", "台灯", new BigDecimal("89.90")));
        products.add(new Product("p6", "纪念徽章", new BigDecimal("15.00")));

        stock.clear();
        stock.put("p1", 100);
        stock.put("p2", 500);
        stock.put("p3", 30);
        stock.put("p4", 20);
        stock.put("p5", 10);
        stock.put("p6", 0);

        carts.clear();
        orders.clear();
        orders.put(1001, new OrderRecord(1001, "alice",
                List.of(new CartItem("p1", "笔记本", new BigDecimal("50.00"), 2)), new BigDecimal("90.00"),
                "学生公寓 3 栋 402", "13800000001", LocalDateTime.of(2026, 9, 1, 10, 0)));
        orders.put(1002, new OrderRecord(1002, "bob",
                List.of(new CartItem("p4", "双肩包", new BigDecimal("129.00"), 1)), new BigDecimal("119.00"),
                "学生公寓 5 栋 117", "13800000002", LocalDateTime.of(2026, 9, 2, 15, 30)));
    }

    public List<User> getUsers() {
        return users;
    }

    public List<Product> getProducts() {
        return products;
    }

    public Product findProduct(String id) {
        return products.stream().filter(p -> p.id().equals(id)).findFirst().orElse(null);
    }

    public synchronized Cart getCart(String user) {
        return carts.computeIfAbsent(user, u -> new Cart());
    }

    public synchronized void addOrder(OrderRecord order) {
        orders.put(order.id(), order);
    }

    public synchronized OrderRecord findOrder(int id) {
        return orders.get(id);
    }

    public synchronized List<OrderRecord> ordersOf(String user) {
        return orders.values().stream()
                .filter(o -> o.user().equals(user))
                .sorted(Comparator.comparingInt(OrderRecord::id))
                .toList();
    }

    // ---- InventoryRepository ----
    @Override
    public synchronized int getStock(String productId) {
        return stock.getOrDefault(productId, 0);
    }

    @Override
    public synchronized void deduct(String productId, int quantity) {
        stock.put(productId, getStock(productId) - quantity);
    }

    @Override
    public synchronized void restore(String productId, int quantity) {
        stock.put(productId, getStock(productId) + quantity);
    }
}
