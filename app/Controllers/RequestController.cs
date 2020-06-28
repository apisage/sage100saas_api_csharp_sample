using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using app.Models;
using app.Repositories;
using Microsoft.AspNetCore.Authentication;

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
            //var model = new Request();
            // Chargement des sociétés dans la liste déroulante.
            Dictionary<string, string> companyOptions = new Dictionary<string, string>();
            companyOptions.Add("$select", "id,name");

            // Gestion du token.
            string accessToken = HttpContext.GetTokenAsync("access_token").Result;
            var repository = APIRepository.Create(accessToken);

            // On récupère la liste des sociétés.
            var message = repository.Get("", "companies", companyOptions);
            if (!Tools.IsSuccess(message))
            {
                model.RespStatusCode = string.Concat((int)message.StatusCode, " - ", message.StatusCode.ToString());
                model.RespBody = Tools.GetStringResult(message);
                return View(model);
            }

            var companies = Tools.GetJSONResult(message);

            if (companies.Count == 0)
            {
                return View(model);
            }

            ViewBag.Companies = companies["value"];

            // Récupération de la première société de sorte à ne pas avoir un champ vide.
            var firstCompanyId = companies["value"][0]["id"].ToString();
            // Autocompletion des ressources d'une société.
            message = repository.Get(firstCompanyId, null, null);

            if (!Tools.IsSuccess(message))
            {
                model.RespStatusCode = string.Concat((int)message.StatusCode, " - ", message.StatusCode.ToString());
                model.RespBody = Tools.GetStringResult(message);
                return View(model);
            }

            var resources = Tools.GetJSONResult(message);

            List<string> endpoints = new List<string>();


            foreach (var resource in resources["value"])
            {
                endpoints.Add(resource["url"].ToString());
            }
            ViewBag.Endpoints = endpoints;

            // Envoi des données du formulaire.
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

            if (!string.IsNullOrEmpty(model.Expand))
            {
                options.Add("$expand", model.Expand);
            }

            if (!string.IsNullOrEmpty(model.Filter))
            {
                options.Add("$filter", model.Filter);
            }

            if (!string.IsNullOrEmpty(model.Select))
            {
                options.Add("$select", model.Select);
            }

            if (!string.IsNullOrEmpty(model.Orderby))
            {
                options.Add("$orderby", model.Orderby);
            }

            if (!string.IsNullOrEmpty(model.Top))
            {
                options.Add("$top", model.Top);
            }

            if (!string.IsNullOrEmpty(model.Skip))
            {
                options.Add("$skip", model.Skip);
            }

            if (!string.IsNullOrEmpty(model.Count))
            {
                options.Add("$count", model.Count);
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
