using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using app.Models;
using app.Repositories;
using Microsoft.AspNetCore.Authentication;
using System.Net;
using app.Settings;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace app.Controllers
{
    public class RequestController : Controller
    {
        /// <summary>
        /// Page d'accueil de Request.
        /// </summary>
        /// <param name="model"> Les données du formulaire si envoyé. </param>
        /// <returns> La vue du formulaire. </returns>
        [HttpGet("Request")]
        public IActionResult Index(Request model)
        {
            //Contrôle token et company et création d'un repository
            var repository = APIRepository.Create(HttpContext.GetTokenAsync("access_token").Result, ApplicationSettings.CompanyId);
            if (repository.ErrorCode == "TOKENEXPIRED") return RedirectToRoute(Tools.forceAuthentication);
            if (repository.ErrorMessage != "") return View("Error", new Error(repository.ErrorMessage));

            return LoadCompaniesAndMetadata(repository,model);
        }

        [HttpPost("Request")]
        public IActionResult ExecuteRequestOrChangeResource(Request model)
        {
            //Contrôle token et company et création d'un repository
            var repository = APIRepository.Create(HttpContext.GetTokenAsync("access_token").Result, ApplicationSettings.CompanyId);
            if (repository.ErrorCode == "TOKENEXPIRED") return RedirectToRoute(Tools.forceAuthentication);
            if (repository.ErrorMessage != "") return View("Error", new Error(repository.ErrorMessage));
            
            ViewResult view= LoadCompaniesAndMetadata(repository, model);

            if (!model.changeResource)
                ExecuteRequest(repository, model);
            
            return view;
        }

        /// <summary>
        /// Récupère la liste des sociétés et les métadonnées
        /// </summary>
        /// <param name="repository">Le repository permettant d'accéder aux données de l'API.</param>
        /// <param name="model">Les données du formulaire</param>
        /// <returns></returns>
        private ViewResult LoadCompaniesAndMetadata(APIRepository repository, Request model)
        { 
            var result = Tools.GetCompanies(repository);
            if (!Tools.IsSuccess(result)) return View("Error", ApplicationSettings.ApiError);

            ViewBag.Companies = result.GetJSONResult()["value"];
            if (ViewBag.Companies.Count == 0)
                return View("Error", new Error(Resource.NOCOMPANIES));

            // Récupération des ressources et sous-ressources via les metadata.
            Dictionary<string, List<string>> resources = Tools.GetMetadataResources(Tools.GetMetadata(repository));
            ViewBag.Resources = resources;

            return View("Index", model);
        }

        /// <summary>
        /// Traite les données du formulaire.
        /// </summary>
        /// <param name="repository"> Le repository permettant d'accéder aux données de l'API. </param>
        /// <param name="model"> Les données du formulaire. </param>
        private void ExecuteRequest(APIRepository repository, Request model)
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
                   options.Add(parameter.Key, parameter.Value);
            }

            var result = repository.Get(model.Company, model.Resource, options, model.ResourceId, model.Subresource); 
            var data = Tools.GetJSONResult(result);
 
            //Tout ce qui est en dessous sert uniquement à formater l'affichage de la réponse dans le formulaire.
            model.RespStatusCode = (int)result.StatusCode;
            model.RespStatusMessage = result.StatusCode.ToString();

            if (data.HasValues)
               model.RespCount = (data.ContainsKey("value")) ? data["value"].Count() : 1; 
            else
                model.RespCount = 0;

            dynamic parsedJson = JsonConvert.DeserializeObject(data.ToString());
  
            model.RespBody = JsonConvert.SerializeObject(parsedJson, Newtonsoft.Json.Formatting.Indented);
        }
    }
}
