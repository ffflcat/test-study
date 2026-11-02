using CampusShop;
using CampusShop.Web;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.WebEncoders;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ShopStore>();
builder.Services.AddSingleton<IInventoryRepository>(sp => sp.GetRequiredService<ShopStore>());
builder.Services.AddSingleton<IPaymentGateway, FakePaymentGateway>();
builder.Services.AddSingleton<INotificationSender, ConsoleNotificationSender>();
builder.Services.AddSingleton(sp => new AuthService(sp.GetRequiredService<ShopStore>().Users));
builder.Services.AddSingleton<PriceCalculator>();
builder.Services.AddSingleton<Pricing>();
builder.Services.AddSingleton<OrderService>();
builder.Services.AddSingleton<CheckoutService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o => o.LoginPath = "/");
builder.Services.AddRazorPages();
// 页面中的中文按原样输出，而不是编码成 &#x...; 形式，便于查看网页源代码
builder.Services.Configure<WebEncoderOptions>(o =>
    o.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new() { Title = "CampusShop API", Version = "v1" });

    // Swagger UI 右上角的 Authorize 按钮：填入登录接口返回的 Token 后，
    // 后续请求会自动带上 Authorization: Bearer <token>。
    o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "输入登录接口返回的 Token（无需手动加 Bearer 前缀）",
    });

    o.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = [],
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.MapShopApi();

app.Run();

/// <summary>让测试项目（WebApplicationFactory）能访问 Program。</summary>
public partial class Program { }
