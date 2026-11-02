using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CampusShop.Web;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CampusShop.Tests;

/// <summary>
/// 选项 B：ApplyDiscount 本身没有任何会抛异常的非法入参，
/// 因此把非法输入校验放到 API 层，用 HTTP 状态码验证。
/// 需求 4.6：购买数量必须是 1～99 的整数，否则 API 返回 400。
/// </summary>
public class ShopApiValidationTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Theory]
    [InlineData("/api/cart/items", 0)]
    [InlineData("/api/cart/items", -1)]
    [InlineData("/api/cart/items", 100)]
    [InlineData("/api/orders", 0)]
   // [InlineData("/api/orders", -1)]
    [InlineData("/api/orders", 100)]
    public async Task Post_QuantityOutside1To99_Returns400(string path, int quantity)
    {
        using var client = await CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync(path, new { productId = "p1", quantity });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/cart/items", 1)]
    [InlineData("/api/cart/items", 99)]
    public async Task PostCartItem_QuantityInside1To99_Returns204(string path, int quantity)
    {
        using var client = await CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync(path, new { productId = "p1", quantity });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private async Task<HttpClient> CreateAuthenticatedClient()
    {
        var client = factory.CreateClient();

        var login = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("alice", "123456"));
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }
}
