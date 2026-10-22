package campusshop;

import org.junit.jupiter.api.Test;

import java.math.BigDecimal;

import static org.junit.jupiter.api.Assertions.assertEquals;

class PriceCalculatorTest {

    @Test
    void applyDiscount_totalIs0_returns0() {
        PriceCalculator calc = new PriceCalculator();

        BigDecimal result = calc.applyDiscount(new BigDecimal("0"));

        assertEquals(0, new BigDecimal("0").compareTo(result));
    }

    @Test
    void applyDiscount_totalIs50_returns50() {
        PriceCalculator calc = new PriceCalculator();

        BigDecimal result = calc.applyDiscount(new BigDecimal("50"));

        assertEquals(0, new BigDecimal("50").compareTo(result));
    }

    @Test
    void applyDiscount_totalIs99_returns99() {
        PriceCalculator calc = new PriceCalculator();

        BigDecimal result = calc.applyDiscount(new BigDecimal("99"));

        assertEquals(0, new BigDecimal("99").compareTo(result));
    }

    @Test
    void applyDiscount_totalIs120_returns110() {
        PriceCalculator calc = new PriceCalculator();

        BigDecimal result = calc.applyDiscount(new BigDecimal("120"));

        assertEquals(0, new BigDecimal("110").compareTo(result));
    }

    @Test
    void applyDiscount_totalIs150_returns140() {
        PriceCalculator calc = new PriceCalculator();

        BigDecimal result = calc.applyDiscount(new BigDecimal("150"));

        assertEquals(0, new BigDecimal("140").compareTo(result));
    }

    @Test
    void applyDiscount_totalIs180_returns170() {
        PriceCalculator calc = new PriceCalculator();

        BigDecimal result = calc.applyDiscount(new BigDecimal("180"));

        assertEquals(0, new BigDecimal("170").compareTo(result));
    }
}
