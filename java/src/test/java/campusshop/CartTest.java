package campusshop;

import org.junit.jupiter.api.Test;

import java.math.BigDecimal;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;

class CartTest {

    private static final Product NOTEBOOK = new Product("p1", "笔记本", new BigDecimal("50.00"));
    private static final Product PEN = new Product("p2", "中性笔", new BigDecimal("2.50"));

    @Test
    void add_twoProducts_subtotalIsSum() {
        Cart cart = new Cart();

        cart.add(NOTEBOOK, 2);
        cart.add(PEN, 4);

        assertEquals(0, new BigDecimal("110.00").compareTo(cart.getSubtotal()));
    }

    @Test
    void add_sameProductTwice_mergesQuantity() {
        Cart cart = new Cart();

        cart.add(NOTEBOOK, 1);
        cart.add(NOTEBOOK, 2);

        assertEquals(1, cart.getItems().size());
        assertEquals(3, cart.getItems().get(0).quantity());
    }

    @Test
    void remove_existingProduct_cartBecomesEmpty() {
        Cart cart = new Cart();
        cart.add(NOTEBOOK, 1);

        cart.remove("p1");

        assertTrue(cart.isEmpty());
    }
}
