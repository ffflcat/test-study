package campusshop.web;

import campusshop.AuthService;
import campusshop.Cart;
import campusshop.Product;
import campusshop.User;
import jakarta.servlet.http.HttpSession;
import org.springframework.stereotype.Controller;
import org.springframework.ui.Model;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestParam;

/**
 * 网页：登录、商品列表、购物车、我的订单。登录状态保存在 Session 中。
 */
@Controller
public class PageController {

    static final String SESSION_USER = "user";

    private final AuthService auth;
    private final ShopStore store;
    private final Pricing pricing;
    private final CheckoutService checkout;

    public PageController(AuthService auth, ShopStore store, Pricing pricing, CheckoutService checkout) {
        this.auth = auth;
        this.store = store;
        this.pricing = pricing;
        this.checkout = checkout;
    }

    @GetMapping("/")
    public String loginPage(HttpSession session) {
        if (session.getAttribute(SESSION_USER) != null)
            return "redirect:/products";
        return "login";
    }

    @PostMapping("/")
    public String login(@RequestParam(required = false) String username,
                        @RequestParam(required = false) String password,
                        HttpSession session, Model model) {
        if (auth.login(username, password) == null) {
            model.addAttribute("username", username);
            model.addAttribute("error", "用户名或密码错误");
            return "login";
        }
        session.setAttribute(SESSION_USER, username.trim());
        return "redirect:/products";
    }

    @PostMapping("/logout")
    public String logout(HttpSession session) {
        session.invalidate();
        return "redirect:/";
    }

    @GetMapping("/products")
    public String products(Model model) {
        model.addAttribute("products", store.getProducts());
        model.addAttribute("store", store);
        return "products";
    }

    @PostMapping("/products")
    public String addToCart(@RequestParam String productId, @RequestParam int quantity,
                            HttpSession session, Model model) {
        Product product = store.findProduct(productId);
        if (product != null) {
            if (quantity < 1 || quantity > ShopApiController.MAX_QUANTITY) {
                model.addAttribute("message", "数量必须在 1 到 " + ShopApiController.MAX_QUANTITY + " 之间");
            } else {
                store.getCart(currentUser(session).username()).add(product, quantity);
                model.addAttribute("message", "已将 " + quantity + " 件" + product.name() + "加入购物车");
            }
        }
        return products(model);
    }

    @GetMapping("/cart")
    public String cart(HttpSession session, Model model) {
        User user = currentUser(session);
        Cart cart = store.getCart(user.username());
        model.addAttribute("cart", cart);
        model.addAttribute("total", pricing.totalFor(user, cart.getSubtotal()));
        return "cart";
    }

    @PostMapping("/cart")
    public String placeOrder(HttpSession session, Model model) {
        CheckoutService.Result result = checkout.checkout(currentUser(session));
        model.addAttribute("placedOrder", result.order());
        model.addAttribute("error", result.error());
        return cart(session, model);
    }

    @GetMapping("/orders")
    public String orders(HttpSession session, Model model) {
        model.addAttribute("orders", store.ordersOf(currentUser(session).username()));
        return "orders";
    }

    private User currentUser(HttpSession session) {
        return auth.findUser((String) session.getAttribute(SESSION_USER));
    }
}
