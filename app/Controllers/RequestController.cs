using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using app.Models;
using app.Repositories;
using Microsoft.AspNetCore.Authentication;
using System.Net;

namespace app.Controllers
{
    public class RequestController : Controller
    {
        /// <summary>
        /// Page d'accueil de Request.
        /// </summary>
        /// <param name="model"> Les données du formulaire si envoyé. </param>
        /// <returns></returns>
        public IActionResult Index(Request model)
        {
            // Chargement des sociétés dans la liste déroulante.
            Dictionary<string, string> companyOptions = new Dictionary<string, string>
            {
                { "$select", "id,name" }
            };

            // Gestion du token.
            string accessToken = HttpContext.GetTokenAsync("access_token").Result;
            var repository = APIRepository.Create(accessToken);

            if (string.IsNullOrEmpty(accessToken))
            {
                Error error = new Error(string.Empty, (int)HttpStatusCode.Unauthorized);
                return View("Error", error);
            }

            // [API01] Récupération de la liste des sociétés.
            var message = repository.Get(string.Empty, "companies", companyOptions);

            if (!Tools.IsSuccess(message))
            {
                var details = "Désolé ! Les sociétés n'ont pas pu être récupérées !";
                Error error = new Error(details, (int)message.StatusCode);

                return View("Error", error);
            }

            var companies = Tools.GetJSONResult(message);

            if (companies.Count == 0)
            {
                return View(model);
            }

            ViewBag.Companies = companies["value"];

            // Récupération de la première société de sorte à ne pas avoir un champ vide.
            var firstCompanyId = companies["value"][0]["id"].ToString();

            // [API02] Récupération des ressources d'une société.
            message = repository.Get(firstCompanyId, null, null);

            if (!Tools.IsSuccess(message))
            {
                var details = "Désolé ! Les ressources des sociétés disponibles n'ont pas pu être récupérées.";
                Error error = new Error(details, (int)message.StatusCode);
                return View("Error");
            }

            var resources = Tools.GetJSONResult(message);
            List<string> endpoints = new List<string>();

            foreach (var resource in resources["value"])
            {
                endpoints.Add(resource["url"].ToString());
            }

            ViewBag.Endpoints = endpoints;

            // [API03] Envoi des données du formulaire.
            if (model.Company != null)
            {
                DoRequest(repository, model);
            }

            return View(model);
        }

        /// <summary>
        /// Traite les données du formulaire.
        /// </summary>
        /// <param name="repository"> Le repository permettant d'accéder aux données de l'API. </param>
        /// <param name="model"> Les données du formulaire. </param>
        private void DoRequest(APIRepository repository, Request model)
        {
            Dictionary<string, string> options = new Dictionary<string, string>();
            Dictionary<string, string> parameters = new Dictionary<string, string>
            {
                { "$expand" , model.Expand },
                { "$filter" , model.Filter},
                { "$select" , model.Select },
                { "$orderby" , model.Orderby },
                { "$top" , model.Top},
                { "$skip" , model.Skip},
                { "$count" , model.Count}
            };

            foreach (var parameter in parameters)
            {
                if (!string.IsNullOrEmpty(parameter.Value) && !string.IsNullOrWhiteSpace(parameter.Value))
                {
                    options.Add(parameter.Key, parameter.Value);
                }
            }

            var message = repository.Get(model.Company, model.Resource, options);

            if (!Tools.IsSuccess(message))
            {
                model.RespStatusCode = string.Concat((int)message.StatusCode, " - ", message.StatusCode.ToString());
                model.RespBody = Tools.GetStringResult(message);
                return;
            }

            // Affichage des résultats.
            model.RespBody = Tools.GetStringResult(message);
            model.RespStatusCode = string.Concat((int)message.StatusCode, " - ", message.StatusCode.ToString());
        }
    }
}
