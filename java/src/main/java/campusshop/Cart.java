package campusshop;

import java.math.BigDecimal;
import java.util.ArrayList;
import java.util.Collections;
import java.util.List;

/**
 * 购物车：同一商品多次加入时合并数量。
 */
public class Cart {

    private final List<CartItem> items = new ArrayList<>();

    public List<CartItem> getItems() {
        return Collections.unmodifiableList(items);
    }

    public boolean isEmpty() {
        return items.isEmpty();
    }

    /** 加入商品；已有该商品时累加数量。 */
    public void add(Product product, int quantity) {
        for (int i = 0; i < items.size(); i++) {
            CartItem item = items.get(i);
            if (item.productId().equals(product.id())) {
                items.set(i, new CartItem(item.productId(), item.name(), item.price(), item.quantity() + quantity));
                return;
            }
        }
        items.add(new CartItem(product.id(), product.name(), product.price(), quantity));
    }

    /** 移除商品；商品不在购物车中时什么也不做。 */
    public void remove(String productId) {
        items.removeIf(i -> i.productId().equals(productId));
    }

    public void clear() {
        items.clear();
    }

    /** 商品原价合计（未打折、未满减）。 */
    public BigDecimal getSubtotal() {
        return items.stream().map(CartItem::getSubtotal).reduce(BigDecimal.ZERO, BigDecimal::add);
    }
}
