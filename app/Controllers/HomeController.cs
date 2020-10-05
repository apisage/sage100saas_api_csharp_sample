using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using app.Repositories;
using app.Settings;
using app.Models;
using System.Linq;

namespace app.Controllers
{
    public class HomeController : Controller
    {
        //****************************[PAGE D'ACCUEIL*******************************************************************
        // Si utilisateur authentifié on renvoie le cache liste des sociétés, si premier appel et pas de cache liste sociétés alors le retour provoque l'appel de loadcompanies.
        public IActionResult Index(bool loadCompanies)
        {
            if (User.Identity.IsAuthenticated)
            {
                //Contrôle token et création d'un repository
                var repository = APIRepository.Create(HttpContext.GetTokenAsync("access_token").Result, "NoCheckCompany");
                if (repository.ErrorCode == "TOKENEXPIRED") return RedirectToRoute(Tools.forceAuthentication);
                if (repository.ErrorMessage != "") return View("Error", new Error(repository.ErrorMessage));

                //Au premier appel on ne passe pas par ce code pour afficher immédiatemment la page sinon on a un écran blanc pendant le temps chargement des sociétés.
                if (loadCompanies || ApplicationSettings.CompaniesCache != null)
                {
                    var result = Tools.GetCompanies(repository);
                    if (!Tools.IsSuccess(result)) return View("Error", ApplicationSettings.ApiError);

                    var companies = result.GetJSONResult()["value"];
                    ViewBag.Companies = companies;
                    if (ViewBag.Companies.Count == 0)
                        return View("Error", new Error(Resource.NOCOMPANIES));

                    if (string.IsNullOrEmpty(ApplicationSettings.CompanyId))
                    {
                        ApplicationSettings.CompanyName = companies[0]["name"].ToString();
                        ApplicationSettings.CompanyId = companies[0]["id"].ToString();
                    }
               }
                else
                    return View("Index");
            }
            //On renvoie un message s'il y en a un stocké préalablement dans MessageInfo -> cas de l'expiration du token
            ViewBag.codeMessage = ApplicationSettings.MessageInfo;
            ApplicationSettings.MessageInfo = null;

            return View("Index");
        }

        //****************************[CHANGEMENT DE SOCIETE]*******************************************************************
        // Modifie la société par défaut d'après les names du formulaire Home : companyid et companyname
        // Ou si demande de vider le cache sociétés, provoque la relecture de la liste des sociétés.
        [HttpPost("/Home/changecompany")]
        public IActionResult ChangeCompany(ChangeCompany changecompany)
        {
            if (changecompany.Clearcache == "on")
                ApplicationSettings.CompaniesCache = null;
            else
            {
                ApplicationSettings.CompanyId = changecompany.CompanyId;
                ApplicationSettings.CompanyName = changecompany.CompanyName;
            }
            return RedirectToRoute(new
            {
                controller = "Home",
                action="Index"
            });
        }

        //****************************[CHANGEMENT DE SOCIETE]*******************************************************************
        // Modifie la société par défaut d'après les names du formulaire Home : companyid et companyname
        public IActionResult Error()
        {
            return View();
        }
    }
}
