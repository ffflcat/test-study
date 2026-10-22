package campusshop;

/** 库存仓库。 */
public interface InventoryRepository {
    int getStock(String productId);

    void deduct(String productId, int quantity);

    void restore(String productId, int quantity);
}
