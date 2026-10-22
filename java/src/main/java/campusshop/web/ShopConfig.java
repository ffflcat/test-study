package campusshop.web;

import campusshop.AuthService;
import campusshop.OrderService;
import campusshop.PriceCalculator;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

/** 把业务类注册为 Spring Bean（业务类本身不依赖 Spring）。 */
@Configuration
public class ShopConfig {

    @Bean
    public PriceCalculator priceCalculator() {
        return new PriceCalculator();
    }

    @Bean
    public AuthService authService(ShopStore store) {
        return new AuthService(store.getUsers());
    }

    @Bean
    public OrderService orderService(FakePaymentGateway pay, ShopStore store, ConsoleNotificationSender notify) {
        return new OrderService(pay, store, notify);
    }
}
