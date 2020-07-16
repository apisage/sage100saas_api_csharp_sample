using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace app.Controllers
{
    public class AuthentificationController : Controller
    {
        public async Task Login()
        {
            if (!User.Identity.IsAuthenticated)
            {
                await HttpContext.ChallengeAsync("SageId", new AuthenticationProperties() { RedirectUri = Url.Action("Index", "Home") });
            }
        }

        [Authorize]
        public async Task Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignOutAsync("SageId", new AuthenticationProperties());
        }
    }
}