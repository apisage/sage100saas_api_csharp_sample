using app.Models;
using app.Settings;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Xml;
using System.Text.RegularExpressions;

namespace app.Repositories
{
    public static class Tools
    {

        public static object forceAuthentication = new
        {
            controller = "Authentification",
            action = "Logout"
        };

        /// <summary>
        /// Détermine si la réponse à une requête est valide ou non.
        /// </summary>
        /// <param name="message"> La réponse à une requête. </param>
        /// <returns> Vrai si la réponse est valide. </returns>
        public static bool IsSuccess(this HttpResponseMessage message)
        {
            if (message.IsSuccessStatusCode)
            {
                return true;
            }
            switch (message.StatusCode)
            {
                case HttpStatusCode.Created: // 201.
                case HttpStatusCode.Accepted: // 202.
                case HttpStatusCode.NonAuthoritativeInformation: // 203.
                case HttpStatusCode.NoContent: // 204.
                case HttpStatusCode.ResetContent: // 205.
                case HttpStatusCode.PartialContent: // 206.
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Vérifie si la réponse d'une requête correspond à un format JSON.
        /// </summary>
        /// <param name="response"> La réponse d'une requête devant être vérifiée. </param>
        /// <returns> Retourne vrai si response est au format JSON. </returns>
        public static bool IsJson(this string response)
        {
            try
            {
                var token = JToken.Parse(response);
                return token.Type == JTokenType.Object || token.Type == JTokenType.Array;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Transforme à partir d'une réponse à une requête, des données JSON en JObject.
        /// </summary>
        /// <param name="message"> La réponse à une requête. </param>
        /// <returns> Un JObject avec les données correspondantes. </returns>
        public static JObject GetJSONResult(this HttpResponseMessage message)
        {
            var response = message.Content.ReadAsStringAsync().Result;
            var result = new JObject();

            if (!string.IsNullOrEmpty(response) && response.IsJson())
            {
                result = JObject.Parse(response);
            }

            return result;
        }
  
        /// <summary>
        /// Arrondit un double à 4 décimales.
        /// </summary>
        /// <param name="value"> La valeur à arrondir. </param>
        /// <returns> La valeur arrondie à 4 décimales. </returns>
        public static double Round(double value)
        {
            return System.Math.Round(value, 4, System.MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Convertit une chaine représentant une date format DDMMYY au format DateTime.
        /// </summary>
        /// <param name="date"> La date DDMMYY à convertir. </param>
        /// <returns> Retourne l'objet DateTime correspondant à la date si valide. </returns>
        public static DateTime ConvertFromTextToDateTime(string date)
        {
            if (string.IsNullOrEmpty(date) || !date.Length.Equals(6))
            {
                return default;
            }

            var day = Int32.Parse(date.Substring(0, 2));
            var month = Int32.Parse(date.Substring(2, 2));
            var year = Int32.Parse(date.Substring(4, 2)) + 2000;

            return new DateTime((int)year, (int)month, (int)day, 0, 0, 0, DateTimeKind.Utc);
        }

        /// <summary>
        /// Renvoie les sociétés disponibles dans lequelles sont possibles des requêtes.
        /// </summary>
        /// <param name="repository"> Le repository. </param>
        /// <returns>Un JObject contenant les sociétés.</returns>
        public static HttpResponseMessage GetCompanies(APIRepository repository)
        {
            if (ApplicationSettings.CompaniesCache == null)
            {
                Dictionary<string, string> options = new Dictionary<string, string>();
                options.Add("$select", "id,name");
                var result = repository.Get(string.Empty, "companies", options);
                ApplicationSettings.CompaniesCache = result;
            }

            return ApplicationSettings.CompaniesCache;
        }

        /// <summary>
        /// Efface le cache des companies et company courante, appelé lors de la connexion utilisateur
        /// </summary>
        public static void ClearCacheCompanies()
        {
            ApplicationSettings.CompaniesCache = null;
            ApplicationSettings.CompanyId = null;
            ApplicationSettings.CompanyName = null;
        }


        /// <summary>
        /// Associe les champs d'un JToken au modèle Customer.
        /// </summary>
        /// <param name="client"> Le JToken à convertir en Customer. </param>
        public static Customer ConvertToCustomer(JToken client)
        {
            var adresse = client["adresse"];
            var telecom = client["telecom"];

            // Compte principal lié au client. La requête OData doit avoir "$expand=comptePrincipal".
            Customer model = new Customer
            {
                Id = client["id"].ToString(),
                Ape = client["ape"].ToString(),
                Classement = client["classement"].ToString(),
                Commentaire = client["commentaire"].ToString(),
                Contact = client["contact"].ToString(),
                Identifiant = client["identifiant"].ToString(),
                Intitule = client["intitule"].ToString(),
                Numero = client["numero"].ToString(),
                Qualite = client["qualite"].ToString(),
                Siret = client["siret"].ToString(),
                Type = client["type"].ToString(),
                ComptePrincipal = client["comptePrincipal"]["numero"].ToString(),
                Adresse = new Address(
                    adresse["adresse"].ToString(),
                    adresse["pays"].ToString(),
                    adresse["complement"].ToString(),
                    adresse["codePostal"].ToString(),
                    adresse["codeRegion"].ToString(),
                    adresse["ville"].ToString()
                    ),
                Telecom = new Telecom(
                    telecom["site"].ToString(),
                    telecom["eMail"].ToString(),
                    telecom["telecopie"].ToString(),
                    telecom["telephone"].ToString()
                    )
            };

            return model;
        }

        public static XmlDocument GetMetadata(APIRepository repository)
        {
            if (ApplicationSettings.MetadataCache == null)
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(ApplicationSettings.UrlApi+repository.CompanyId+"/$metadata");
                ApplicationSettings.MetadataCache = doc;
            }
            return ApplicationSettings.MetadataCache;
        }

        public static Dictionary<string, MainResources> GetMetadataResources(XmlDocument document)
        {
            if (ApplicationSettings.MetadataCacheResources == null)
            {
                XmlNamespaceManager manager = new XmlNamespaceManager(document.NameTable);
                manager.AddNamespace("edmx", document.DocumentElement.NamespaceURI);
                manager.AddNamespace("model", "http://docs.oasis-open.org/odata/ns/edm");

                Dictionary<string, MainResources> resources = new Dictionary<string, MainResources>();
                
                var xpath = "/edmx:Edmx/edmx:DataServices/model:Schema/model:EntityContainer/*";
                XmlNodeList entityContainer = document.SelectNodes(xpath, manager);

                foreach (XmlNode entity in entityContainer)
                {
                    var resourceName = entity.Attributes["Name"].Value;

                    // Récupération des relations.
                    var relations = entity.SelectNodes("model:NavigationPropertyBinding", manager);                    
                    var subresource = new List<string>();

                    foreach (XmlNode node in relations)
                    {
                        var path = node.Attributes["Path"].Value;
                        if (!path.StartsWith("100S.Model."))
                            subresource.Add(path);
                    }

                    // Récupération des sous-ressources.
                    var nonExpandableChild = entity.LastChild;
                    if (nonExpandableChild!=null && nonExpandableChild.Name.Equals("Annotation"))
                    {
                        var xpathSubresources = "model:Record/model:PropertyValue/model:Collection/model:NavigationPropertyPath/text()";
                        var tagCollection = nonExpandableChild.SelectNodes(xpathSubresources, manager);

                        foreach (XmlNode node in tagCollection)
                        {
                            if (!subresource.Exists(x => x.Equals(node.InnerText)))
                                subresource.Add(node.InnerText);
                        }
                    }
                    resources[resourceName] = MainResources.Create(subresource, entity.Name);

                }
                ApplicationSettings.MetadataCacheResources = resources;
            }
            return ApplicationSettings.MetadataCacheResources;
        }

        public class MainResources
        {
            public static MainResources Create(List<string> relations, string entitytype)
            {
                return new MainResources
                {
                    Relations = relations,
                    EntityType = entitytype
                };
            }
            public List<string> Relations { get; set; }
            public string EntityType { get; set; }
        }

        /// <summary>
        /// Retourne une URL correspondant à un élément de type single-valued relation.
        /// </summary>
        /// <param name="repository"> Le repository. </param>
        /// <param name="resource"> Le nom de la ressource dont on veut la liaison. </param>
        /// <param name="toBind"> L'id</param>
        /// <returns> Une chaîne. </returns>
        public static string OdataBind(APIRepository repository, string resource, string toBind)
        {
           return ApplicationSettings.UrlApi+repository.CompanyId+"/"+resource+"('"+toBind+"')";
        }

        /// <summary>
        /// Définit le message d'erreur issue du résultat d'une requête issu d'un json erreur API ou d'une page html de type 404 par exemple.
        /// </summary>
        /// <param name="result"> Le résultat issue d'une requête. </param>
        //Context + result.StatusCode/ <param name="Context">Optionnellement l'url de la route de l'API appelée</param>
        public static string FormateErrorApi(HttpResponseMessage result,string Context=null)
        {       
            Context = (Context == null) ? "":"<div class='errorApi'>"+Context + "</div><br>";

            var res = result.Content.ReadAsStringAsync().Result;
            if (res == "")
                return Context + (int)result.StatusCode + " - " + result.StatusCode;

            if (IsJson(res))
            {
                var message = JsonConvert.DeserializeObject<JObject>(res);
                if (message["Message"] != null)
                    return (Context + message["Message"]+" ("+result.ReasonPhrase+")").Replace("\r\n", "");
                else
                    return (Context + message["error"]["code"] + " (" + result.ReasonPhrase + ") - " + message["error"]["message"].ToString()).Replace("\r\n", "");
            }
            else
            {
                var response = Regex.Match(res, "<title>(.*)</title>", RegexOptions.Singleline).Groups[1].Value;
                if (response == "")
                    response = res;
                return Context + response;
            }
        }

    }
}
