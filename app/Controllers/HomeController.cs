using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net;
using app.Models;
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
                if (!String.IsNullOrEmpty(access_token) && String.IsNullOrEmpty(HttpContext.Session.GetString("companyId")))
                {
                    var repository = APIRepository.Create(access_token);
                    Dictionary<string, string> companyOptions = new Dictionary<string, string>();
                    companyOptions.Add("$filter", String.Format("name eq '{0}'", ApplicationSettings.company_name));
                    var message = repository.Get("", "companies", companyOptions);
                    if (!Tools.IsSuccess(message))
                    {
                        ViewBag.ErrorMessage = string.Concat((int)message.StatusCode, " - ", message.StatusCode.ToString(), Environment.NewLine, Tools.GetStringResult(message));
                        return;
                    }

                    var companies = Tools.GetJSONResult(message);

                    if (companies.Count == 0 || companies["value"].Count() == 0)
                    {
                        ViewBag.ErrorMessage = $"La société {ApplicationSettings.company_name} n'existe pas ou vous ne disposez pas des autorisations pour y accéder";
                        return;
                    }

                    var firstCompanyId = companies["value"][0]["id"].ToString();
                    HttpContext.Session.SetString("companyId", firstCompanyId);
                    HttpContext.Session.SetString("companyName", ApplicationSettings.company_name);
                }
            }
        }
    }
}
