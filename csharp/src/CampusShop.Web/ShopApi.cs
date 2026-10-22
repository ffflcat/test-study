using CampusShop;

namespace CampusShop.Web;

public record LoginRequest(string Username, string Password);
public record LoginResponse(string Token, int ExpiresIn);
public record ErrorResponse(string Error);
public record ProductDto(string Id, string Name, decimal Price, int Stock);
public record CartItemRequest(string ProductId, int Quantity);
public record CartDto(List<CartItem> Items, decimal Subtotal, decimal Total);
public record CreateOrderRequest(string ProductId, int Quantity);
public record OrderDto(int Id, string User, List<CartItem> Items, decimal Total,
    string Address, string Phone, DateTime CreatedAt);

/// <summary>
/// REST API。除登录和商品列表外，都需要请求头 Authorization: Bearer &lt;token&gt;。
/// 接口说明见 /swagger。
/// </summary>
public static class ShopApi
{
    public const int MaxQuantity = 99;

    public static void MapShopApi(this WebApplication app)
    {
        var api = app.MapGroup("/api");

        api.MapPost("/auth/login", (LoginRequest req, AuthService auth) =>
        {
            var token = auth.Login(req.Username, req.Password);
            return token is null
                ? Results.Json(new ErrorResponse("用户名或密码错误"), statusCode: 401)
                : Results.Ok(new LoginResponse(token, AuthService.TokenLifetimeSeconds));
        })
        .WithSummary("登录，返回 Token")
        .Produces<LoginResponse>(200)
        .Produces<ErrorResponse>(400)
        .Produces<ErrorResponse>(401);

        api.MapGet("/products", (ShopStore store) =>
            store.Products.Select(p => new ProductDto(p.Id, p.Name, p.Price, store.GetStock(p.Id))))
        .WithSummary("商品列表");

        api.MapGet("/cart", (HttpContext http, ShopStore store, AuthService auth, Pricing pricing) =>
        {
            if (CurrentUser(http, auth) is not { } user) return Results.Unauthorized();
            var cart = store.GetCart(user.Username);
            return Results.Ok(new CartDto(cart.Items.ToList(), cart.Subtotal, pricing.TotalFor(user, cart.Subtotal)));
        })
        .WithSummary("查看购物车")
        .Produces<CartDto>(200);

        api.MapPost("/cart/items", (CartItemRequest req, HttpContext http, ShopStore store, AuthService auth) =>
        {
            if (CurrentUser(http, auth) is not { } user) return Results.Unauthorized();
            if (req.Quantity <= 0 || req.Quantity > MaxQuantity)
                return Results.BadRequest(new ErrorResponse($"数量必须在 1 到 {MaxQuantity} 之间"));
            if (store.FindProduct(req.ProductId) is not { } product)
                return Results.NotFound(new ErrorResponse("商品不存在"));
            store.GetCart(user.Username).Add(product, req.Quantity);
            return Results.NoContent();
        })
        .WithSummary("加入购物车");

        api.MapDelete("/cart/items/{productId}", (string productId, HttpContext http, ShopStore store, AuthService auth) =>
        {
            if (CurrentUser(http, auth) is not { } user) return Results.Unauthorized();
            store.GetCart(user.Username).Remove(productId);
            return Results.NoContent();
        })
        .WithSummary("从购物车移除商品");

        api.MapPost("/orders", (CreateOrderRequest req, HttpContext http, ShopStore store,
            AuthService auth, Pricing pricing, OrderService orders) =>
        {
            if (CurrentUser(http, auth) is not { } user) return Results.Unauthorized();
            if (req.Quantity == 0 || req.Quantity > MaxQuantity)
                return Results.BadRequest(new ErrorResponse($"数量必须在 1 到 {MaxQuantity} 之间"));
            if (store.FindProduct(req.ProductId) is not { } product)
                return Results.NotFound(new ErrorResponse("商品不存在"));

            var subtotal = product.Price * req.Quantity;
            var total = pricing.TotalFor(user, subtotal);
            var result = orders.PlaceOrder(new Order(user.Username, product.Id, req.Quantity, total));
            switch (result.Status)
            {
                case OrderStatus.InsufficientStock:
                    return Results.Conflict(new ErrorResponse(result.Message));
                case OrderStatus.PaymentFailed:
                    return Results.Json(new ErrorResponse(result.Message), statusCode: 402);
            }

            var record = new OrderRecord(int.Parse(result.OrderId!), user.Username,
                new() { new CartItem(product.Id, product.Name, product.Price, req.Quantity) },
                total, user.Address, user.Phone, DateTime.Now);
            store.AddOrder(record);
            return Results.Created($"/api/orders/{record.Id}", ToDto(record));
        })
        .WithSummary("直接下单（单个商品）")
        .Produces<OrderDto>(201)
        .Produces<ErrorResponse>(400)
        .Produces<ErrorResponse>(402)
        .Produces<ErrorResponse>(404)
        .Produces<ErrorResponse>(409);

        api.MapGet("/orders", (HttpContext http, ShopStore store, AuthService auth) =>
        {
            if (CurrentUser(http, auth) is not { } user) return Results.Unauthorized();
            return Results.Ok(store.OrdersOf(user.Username).Select(ToDto));
        })
        .WithSummary("我的订单")
        .Produces<List<OrderDto>>(200);

        api.MapGet("/orders/{id:int}", (int id, HttpContext http, ShopStore store, AuthService auth) =>
        {
            if (CurrentUser(http, auth) is null) return Results.Unauthorized();
            if (store.FindOrder(id) is not { } order)
                return Results.NotFound(new ErrorResponse("订单不存在"));
            return Results.Ok(ToDto(order));
        })
        .WithSummary("查看订单详情")
        .Produces<OrderDto>(200)
        .Produces<ErrorResponse>(404);

        if (app.Environment.IsDevelopment())
        {
            api.MapPost("/test/reset", (ShopStore store) =>
            {
                store.Reset();
                return Results.NoContent();
            })
            .WithSummary("（仅开发环境）把数据恢复为初始状态");
        }
    }

    private static OrderDto ToDto(OrderRecord o) =>
        new(o.Id, o.User, o.Items, o.Total, o.Address, o.Phone, o.CreatedAt);

    /// <summary>从请求头 Authorization: Bearer &lt;token&gt; 中取出当前用户。</summary>
    private static User? CurrentUser(HttpContext http, AuthService auth)
    {
        var header = http.Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;
        var name = auth.Validate(header["Bearer ".Length..].Trim());
        return name is null ? null : auth.FindUser(name);
    }
}
