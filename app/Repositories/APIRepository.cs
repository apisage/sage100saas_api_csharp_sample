using app.Settings;
using app.Models;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace app.Repositories
{
    public class APIRepository
    {
        public string CompanyId{get;set;}
        public string Token{get;set;}
        public string ErrorMessage{get;set;}
        public string ErrorCode{get;set;}
        public bool noInitializeHistoApis { get; set; }


        public static DateTime startCallApi { get; set; }
  
        /// <summary>
        /// Récupération des données dans une collection (ex: comptes, clients...).
        /// </summary>
        /// <param name="company"> L'id de société dans laquelle la récupération aura lieu. </param>
        /// <param name="resource"> La ressource souhaitée. </param>
        /// <param name="options"> Les paramètres OData (count, filter...) étant utilisés pour obtenir une réponse précise. </param>
        /// <returns> Un message http contenant les données au format JObject ainsi que le code réponse de la requête. </returns>
        public HttpResponseMessage Get(string company, string resource = null, Dictionary<string, string> options = null, string id = null, string subResource = null)
        {
            var uri = ApplicationSettings.UrlApi+company;

            if (!string.IsNullOrEmpty(resource))
            {
                var slash = string.IsNullOrEmpty(company) ? "" : "/";
                uri = string.Concat(uri, slash, resource);
            }

            if(!string.IsNullOrEmpty(id))
                uri = string.Concat(uri, "('", id, "')");

            if(!string.IsNullOrEmpty(subResource))
                 uri = string.Concat(uri, "/", subResource);
 
            // Ajout des options et suppression des options non affectées
            if (options != null)
            {
                foreach (var option in options)
                {
                    if (option.Value == "") options.Remove(option.Key);
                }
                uri = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(uri, options);
            }

            var client = CreateHttpClientAndInitializeParams();

            // Récupération des données de l'uri.
            var result = client.GetAsync(uri).Result;
            saveErrorAndTime(result, uri);
            return result;
        }

        /// <summary>
        /// Insère des données dans une collection selon l'id d'une société.
        /// </summary>
        /// <param name="datasetId"> La société dans laquelle la ressource sera insérée. </param>
        /// <param name="resourceName"> Le nom de la collection dans laquelle les données seront insérées (ex: clients). </param>
        /// <param name="data"> Les valeurs de la ressource insérée. </param>
        /// <returns> Le résultat HttpResponseMessage. </returns>
        public HttpResponseMessage Post(string datasetId, string resourceName, string data)
        {
            var client = CreateHttpClientAndInitializeParams();
            var uri = string.Concat(ApplicationSettings.UrlApi, datasetId, "/", resourceName);
            var result = client.PostAsync(uri, new StringContent(data, Encoding.UTF8, "application/json")).Result;
            saveErrorAndTime(result, uri);
            return result;
        }

        /// <summary>
        /// Met à jour une ressource dans une collection (ex: clients('{id}')) selon un id de société avec de nouvelles données.
        /// </summary>
        /// <param name="datasetId"> La collection dans laquelle la mise à jour va s'effectuer. </param>
        /// <param name="resourceName"> La ressource qui va être mise à jour (ex: clients('{id}'). </param>
        /// <param name="data"> Les nouvelles valeurs. </param>
        /// <returns> Le résultat HttpResponseMessage. </returns>
        public HttpResponseMessage Patch(string datasetId, string resourceName, string data, string resourceId = null)
        {
            var client = CreateHttpClientAndInitializeParams();
            var uri = string.Concat(ApplicationSettings.UrlApi, datasetId, "/", resourceName);

            if(!string.IsNullOrEmpty(resourceId))
            {
                uri = string.Concat(uri, "('", resourceId, "')");
            }
            var result = client.PatchAsync(uri, new StringContent(data, Encoding.UTF8, "application/json")).Result;
            saveErrorAndTime(result, uri);
            return result;
       }

        /// <summary>
        /// Supprime dans une ressource un élément d'une collection.
        /// </summary>
        /// <param name="datasetId"> L'id de la société dans laquelle on souhaite supprimer. </param>
        /// <param name="resourceName"> Le nom de la ressource, souvent associé de l'id (ex: clients('{id}'). </param>
        /// <returns></returns>
        public HttpResponseMessage Delete(string datasetId, string resourceName, string resourceId = null)
        {
            var client = CreateHttpClientAndInitializeParams();
            var uri = string.Concat(ApplicationSettings.UrlApi, datasetId, "/", resourceName);

            if (!string.IsNullOrEmpty(resourceId))
            {
                uri = string.Concat(uri, "('", resourceId, "')");
            }

            var result = client.DeleteAsync(uri).Result;

            saveErrorAndTime(result, uri);
            return result;
        }

        /// <summary>
        /// Configure un client Http.
        /// </summary>
        /// <returns> Un client configuré. </returns>
        private HttpClient CreateHttpClientAndInitializeParams()
        {
            ApplicationSettings.ApiError = null;
            startCallApi = DateTime.Now;

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return client;
        }

        private void saveErrorAndTime(HttpResponseMessage result,string uri)
        {
            if (!Tools.IsSuccess(result)) ApplicationSettings.ApiError = new Error(Tools.FormateErrorApi(result, HttpUtility.UrlDecode(uri)));

            //Trace appels Api, le flag noInitializeHistoApis évite de perturber avec les appels Ajax asynchrones exécutés en complément duc hargemebt de la page
            if (!noInitializeHistoApis)
            {
                TimeSpan diff = DateTime.Now - startCallApi;
                ApplicationSettings.LastAPIs.Add(new HistoApis(HttpUtility.UrlDecode(uri), Math.Truncate(diff.TotalSeconds * 100) / 100));
            }
         }

        public class HistoApis
        {
            public HistoApis(string uri,double duration)
            {
                this.uri = uri;
                this.duration = duration;
            }
            public string uri { get; set; }
            public double duration { get; set; }
        }

        private APIRepository()
        {
        }

        /// <summary>
        /// Retourne une instance APIRepository 
        /// </summary>
        /// <param name="token"> Le jeton d'authentification nécessaire. </param>
        /// <param name="companyId"> L'id company courant </param>
        /// <param name="noInitializeHistoApis">true ne réinitialise pas la liste histo Api, utilisé pour neutraliser impact des appels Ajax asynchrones</param>
        /// <returns></returns>
        public static APIRepository Create(string token, string companyId,bool noInitializeHistoApis=false)
        {
            if (!noInitializeHistoApis) ApplicationSettings.LastAPIs = new List<HistoApis>();

            //Analyse des erreurs potentielles
            var errorCode = "";
            var errorMessage = "";
            if (string.IsNullOrEmpty(token))
                errorCode = "NOTOKEN";
            else if (string.IsNullOrEmpty(companyId))
                errorCode = "NOCOMPANY";
            else
            {
                var jwt = new JwtSecurityToken(jwtEncodedString: token);
                if (jwt.ValidTo <= DateTime.Now)
                    errorCode = "TOKENEXPIRED";
            }

            if (errorCode != "")
            {
                errorMessage= Resource.ResourceManager.GetString(errorCode);
                if (errorCode == "TOKENEXPIRED")
                    ApplicationSettings.MessageInfo = errorMessage;
            }

            return new APIRepository
            {
                Token = token,
                CompanyId = companyId,
                ErrorMessage = errorMessage,
                ErrorCode = errorCode,
                noInitializeHistoApis=noInitializeHistoApis
            };

        }
    }
}
