using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using app.Repositories;
using app.Settings;

namespace app.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            SetCompanyId();
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }

        private void SetCompanyId()
        {
            if (User.Identity.IsAuthenticated)
            {
                var access_token = HttpContext.GetTokenAsync("access_token").Result;
                var name = ApplicationSettings.CompanyName;

                if (!string.IsNullOrEmpty(access_token) && !string.IsNullOrEmpty(name))
                {
                    var repository = APIRepository.Create(access_token);
                    Dictionary<string, string> companyOptions = new Dictionary<string, string>
                    {
                        { "$filter", string.Format("name eq '{0}'", name) }
                    };

                    var message = repository.Get("", "companies", companyOptions);
                    if (!Tools.IsSuccess(message))
                    {
                        ViewBag.ErrorMessage = string.Concat((int)message.StatusCode, " - ", message.StatusCode.ToString(), Environment.NewLine, Tools.GetStringResult(message));
                        return;
                    }

                    var companies = Tools.GetJSONResult(message);

                    if (companies.Count == 0 || companies["value"].Count() == 0)
                    {
                        ViewBag.ErrorMessage = $"La société {name} n'existe pas ou vous ne disposez pas des autorisations pour y accéder";
                        return;
                    }

                    var firstCompanyId = companies["value"][0]["id"].ToString();
                    ApplicationSettings.CompanyName = name;
                    ApplicationSettings.CompanyId = firstCompanyId;
                }
            }
        }
    }
}
