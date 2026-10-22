using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CampusShop.Web.Pages;

/// <summary>需要登录的页面的基类。</summary>
[Authorize]
public abstract class ShopPageModel(AuthService auth) : PageModel
{
    protected User CurrentUser => auth.FindUser(User.Identity!.Name!)!;
}
