package campusshop.web;

import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import org.springframework.context.annotation.Configuration;
import org.springframework.web.servlet.HandlerInterceptor;
import org.springframework.web.servlet.config.annotation.InterceptorRegistry;
import org.springframework.web.servlet.config.annotation.WebMvcConfigurer;

/** 未登录时访问商品、购物车、订单页面，跳转到登录页。 */
@Configuration
public class WebConfig implements WebMvcConfigurer {

    @Override
    public void addInterceptors(InterceptorRegistry registry) {
        registry.addInterceptor(new HandlerInterceptor() {
            @Override
            public boolean preHandle(HttpServletRequest request, HttpServletResponse response, Object handler)
                    throws Exception {
                var session = request.getSession(false);
                if (session == null || session.getAttribute(PageController.SESSION_USER) == null) {
                    response.sendRedirect("/");
                    return false;
                }
                return true;
            }
        }).addPathPatterns("/products", "/cart", "/orders");
    }
}
