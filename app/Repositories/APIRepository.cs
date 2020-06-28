using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace app.Repositories
{
    public class APIRepository
    {
        public String _BASE_URL = "https://api-dev-sage100saas.sagedatacloud.com/";
        //public String _BASE_URL = "http://localhost:62005/api/V1/";
        private readonly static Lazy<APIRepository> _instance = new Lazy<APIRepository>(() => new APIRepository());

        private string Token
        {
            get;
            set;
        }


        /// <summary>
        /// Récupération des données dans une collection (ex: comptes, clients...).
        /// </summary>
        /// <param name="company"> L'id de société dans laquelle la récupération aura lieu. </param>
        /// <param name="resource"> La ressource souhaitée. </param>
        /// <param name="options"> Les paramètres OData (count, filter...) étant utilisés pour obtenir une réponse précise. </param>
        /// <returns> Un tuple contenant les données au format JObject ainsi que le code réponse de la requête. </returns>
        public HttpResponseMessage Get(string company, string resource = null, Dictionary<string, string> options = null)
        {
            var uri = string.Concat(_BASE_URL, company);//, "/", resource);

            if (!string.IsNullOrEmpty(resource))
            {
                var slash = string.IsNullOrEmpty(company) ? "" : "/";
                uri = string.Concat(uri, slash, resource);
            }
            // Ajout des options.
            if (options != null)
            {
                uri = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(uri, options);
            }

            var client = CreateHttpClient();

            // Récupération des données de l'uri.
            return client.GetAsync(uri).Result;
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
            var client = CreateHttpClient();
            var uri = String.Concat(_BASE_URL, datasetId, "/", resourceName);
            var request = client.PostAsync(uri, new StringContent(data, Encoding.UTF8, "application/json"));
            return request.Result;
        }

        /// <summary>
        /// Met à jour une ressource dans une collection (ex: clients('{id}')) selon un id de société avec de nouvelles données.
        /// </summary>
        /// <param name="datasetId"> La collection dans laquelle la mise à jour va s'effectuer. </param>
        /// <param name="resourceName"> La ressource qui va être mise à jour (ex: clients('{id}'). </param>
        /// <param name="data"> Les nouvelles valeurs. </param>
        /// <returns> Le résultat HttpResponseMessage. </returns>
        public HttpResponseMessage Patch(string datasetId, string resourceName, string data)
        {
            var client = CreateHttpClient();
            var uri = String.Concat(_BASE_URL, datasetId, "/", resourceName);
            var request = client.PatchAsync(uri, new StringContent(data, Encoding.UTF8, "application/json"));
            return request.Result;
        }

        /// <summary>
        /// Configure un client Http.
        /// </summary>
        /// <returns> Un client configuré. </returns>
        private HttpClient CreateHttpClient()
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }

        /// <summary>
        /// Lecture du fichier de configuration.
        /// </summary>
        /// <param name="context"></param>
        /*public void TokenfileRead(HttpContext context)
        {
            if (System.IO.File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "access_token.json")))
            {
                using (StreamReader file = System.IO.File.OpenText(Path.Combine(Directory.GetCurrentDirectory(), "access_token.json")))
                using (JsonTextReader reader = new JsonTextReader(file))
                {
                    JObject jsonObj = (JObject)JToken.ReadFrom(reader);
                    context.Request.HttpContext.Session.SetString("access_token", (string)jsonObj["access_token"]);
                    context.Request.HttpContext.Session.SetString("expires_at", (string)jsonObj["expires_at"]);
                    context.Request.HttpContext.Session.SetString("refresh_token", (string)jsonObj["refresh_token"]);
                    context.Request.HttpContext.Session.SetString("refresh_token_expires_at", (string)jsonObj["refresh_token_expires_at"]);
                }
            }
        }*/

        private APIRepository()
        {
        }

        /// <summary>
        /// Retourne une instance APIRepository.
        /// </summary>
        /// <param name="token"> Le jeton d'authentification nécessaire. </param>
        /// <returns></returns>
        public static APIRepository Create(string token)
        {
            var instance = new APIRepository();
            instance.Token = token;
            return instance;
        }
    }
}
