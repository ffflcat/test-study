using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CampusShop.Web.Pages;

public class IndexModel(AuthService auth) : PageModel
{
    [BindProperty] public string Username { get; set; } = "";
    [BindProperty] public string Password { get; set; } = "";
    public string? Error { get; set; }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
            return Redirect("/Products");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (auth.Login(Username, Password) is null)
        {
            Error = "用户名或密码错误";
            return Page();
        }
        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Name, Username.Trim()) },
            CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(new ClaimsPrincipal(identity));
        return Redirect("/Products");
    }
}
