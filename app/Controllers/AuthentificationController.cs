using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using app.Repositories;
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
                await HttpContext.ChallengeAsync("Auth0", new AuthenticationProperties() { RedirectUri = Url.Action("Index", "Home") });
            }
        }

        [Authorize]
        public async Task Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignOutAsync("Auth0", new AuthenticationProperties());
        }
    }
}