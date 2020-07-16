using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using app.Models;
using app.Repositories;
using Microsoft.AspNetCore.Authentication;
using Newtonsoft.Json.Serialization;
using app.Settings;
using System.Net;
using Microsoft.Extensions.Options;

namespace app.Controllers
{
    public class CustomersController : Controller
    {
        private readonly string _resourceName = "clients";

        /// <summary>s
        /// Affiche la vue principale Customers.
        /// </summary>
        /// <returns> La vue Customers. </returns>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Index(string searchTerm = "", string skip = "0", string top = "10")
        {
            // Persistence du token et de la société.
            string accessToken = HttpContext.GetTokenAsync("access_token").Result;
            var repository = APIRepository.Create(accessToken);

            var companyId = ApplicationSettings.CompanyId;
            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(companyId))
            {
                Error error = new Error(string.Empty, (int)HttpStatusCode.Unauthorized);
                return View("Error", error);
            }

            // Construction de la requête de recherche OData.
            // [API04] Récupération des clients selon le champ de recherche.
            Dictionary<string, string> options = new Dictionary<string, string>();
            var qryCount = "true";
            var qrySkip = skip ?? "0";
            var qryTop = "0";
            var qryOrderby = "intitule,numero";
            var qrySelect = "id,intitule,numero,adresse,telecom/telephone";
            var qryFilter = (string.IsNullOrEmpty(searchTerm)) ? null : "contains(tolower(intitule), '{searchTerm}') or contains(tolower(numero), '{searchTerm}')";
            options.Add("$count", qryCount);
            options.Add("$top", qryTop);

            if (!string.IsNullOrEmpty(qryFilter))
            {
                qryFilter = qryFilter.Replace("{searchTerm}", searchTerm.ToLower());
                options.Add("$filter", qryFilter);
            }

            // Une requête avec count séparé est avantageux pour les performances.
            var messageCount = repository.Get(companyId, _resourceName, options);

            if (!Tools.IsSuccess(messageCount))
            {
                var details = "Désolé ! La liste des clients n'a pas pu être récupéré.";
                Error error = new Error(details);

                return View("Error", error);
            }

            var count = messageCount.GetJSONResult()["@odata.count"].ToString();
            options["$top"] = top;
            options.Remove("$count");
            options.Add("$skip", qrySkip);
            options.Add("$orderby", qryOrderby);
            options.Add("$select", qrySelect);
            var message = repository.Get(companyId, _resourceName, options);

            if (!Tools.IsSuccess(message))
            {
                var details = "Désolé ! La liste des clients n'a pas pu être récupéré.";
                Error error = new Error(details);

                return View("Error", error);
            }

            ViewBag.Results = Int32.Parse(count);
            ViewBag.ResultsByPage = Int32.Parse(top);
            ViewBag.Clients = message.GetJSONResult();
            ViewBag.SearchTerm = searchTerm;
            ViewBag.options = options;
            var page = Int32.Parse(qrySkip);
            ViewBag.CurrentPage = (page != 0) ? (page) : 1;

            return View();
        }

        /// <summary>
        /// Affiche et gère la formulaire d'ajout de Customer.
        /// </summary>
        /// <returns> La vue avec le nouveau Customer. </returns>
        [Route("Customers/add"), ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Add(Customer customer, Address address, Telecom telecom)
        {
            ViewBag.Action = "add";

            // Gestion du token.
            string accessToken = HttpContext.GetTokenAsync("access_token").Result;
            var repository = APIRepository.Create(accessToken);

            var companyId = ApplicationSettings.CompanyId;
            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(companyId))
            {
                HttpContext.Response.Redirect("/");
                return View();
            }

            // [API06] Récupération des comptes généraux dans le formulaire d'ajout.
            var accounts = GetActiveAccountsStartingWithNumber(repository, companyId, "41");
            ViewBag.Accounts = accounts["value"];

            Customer model = new Customer
            {
                Adresse = address,
                Telecom = telecom
            };

            bool customerIsNull = Customer.IsNull(customer);
            bool customerIsEmpty = Customer.IsEmpty(customer);

            if (customerIsNull)
            {
                return View(model);
            }

            // Lie les données adresse et telecom au client.
            customer.Adresse = address;
            customer.Telecom = telecom;

            // Les valeurs du formulaire ne seront pas réinitialisés même s'il y a une erreur.
            model = customer;

            // Le formulaire a été envoyé.
            if (customerIsEmpty)
            {
                ViewBag.ErrorMessage = "Le numéro, l'intitulé ou le type ne peuvent être des champs vides.";
                return View(model);
            }

            customer.Numero = customer.Numero.ToUpper();

            // Il ne peut exister deux clients avec le même compte tiers (Numero).
            Dictionary<string, string> optionsDuplicate = new Dictionary<string, string>();
            var numeroFilter = "numero eq '{numero}'";
            numeroFilter = numeroFilter.Replace("{numero}", customer.Numero);
            optionsDuplicate.Add("$filter", numeroFilter);
            var messageDuplicate = repository.Get(companyId, _resourceName, optionsDuplicate);

            if (Tools.IsSuccess(messageDuplicate))
            {
                var duplicate = messageDuplicate.GetJSONResult()["value"];

                if (duplicate.HasValues)
                {
                    ViewBag.ErrorMessage = "Ce compte tiers est déjà enregistré. Il ne peut exister de doublons.";
                    return View(model);
                }
            }

            // [API07] Ajout des données du client - Envoi des données.
            var accountId = "/comptes('{accountId}')";
            accountId = accountId.Replace("{accountId}", customer.ComptePrincipal);


            // Liaison du compte principal au client à ajouter.
            customer.ComptePrincipal = string.Concat(repository._BASE_URL, companyId, accountId);

            // Conversion de Customer en données exploitables par la méthode POST.
            var data = JsonConvert.SerializeObject(customer, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            // Envoi des données pour l'ajout.
            var result = repository.Post(companyId, _resourceName, data);

            // Gestion d'erreurs.
            if (!result.IsSuccessStatusCode)
            {
                var reason = result.ReasonPhrase;
                var explanation = result.Content.ReadAsStringAsync().Result;
                var message = JsonConvert.DeserializeObject<JObject>(explanation);
                var error = string.Concat(message["error"]["code"], " (", reason, ")", " - ", message["error"]["message"].ToString());
                ViewBag.ErrorMessage = error;
            }
            else
            {
                // Redirection.
                return RedirectToRoute(new
                {
                    controller = "Customers",
                    action = "Index",
                    searchTerm = customer.Intitule
                });
            }

            return View(model);
        }

        /// <summary>
        /// Affiche les informations d'un client.
        /// </summary>
        /// <param name="id"> Id du client dont on souhaite consulter les informations. </param>
        /// <returns> La vue du cient. </returns>
        [Route("Customers/show/{id}"), ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Get(string id)
        {
            // Construction de la requête OData afin d'obtenir une requête précise.
            Dictionary<string, string> options = new Dictionary<string, string>();
            var filter = "id eq ('{customerId}')";
            filter = filter.Replace("{customerId}", id);
            options.Add("$filter", filter);

            // Gestion du token.
            string accessToken = HttpContext.GetTokenAsync("access_token").Result;
            var repository = APIRepository.Create(accessToken);

            var companyId = ApplicationSettings.CompanyId;
            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(companyId))
            {
                HttpContext.Response.Redirect("/");
                return View();
            }

            // [API05] Récupération des données pour la consultation d'une fiche client.
            var details = repository.Get(companyId, _resourceName, options).GetJSONResult();
            Customer model = ConvertToCustomer(companyId, details);

            return View(model);
        }

        /// <summary>
        /// Mise à jour d'un client.
        /// </summary>
        /// <param name="customer"> Contient les nouvelles valeurs pour chaque propriété non objet. </param>
        /// <param name="address"> Contient les nouvelles valeurs pour l'objet adresse.</param>
        /// <param name="telecom"> Contient les nouvelles valeurs pour l'objet telecom.</param>
        /// <returns> The view of a customer. </returns>
        [Route("Customers/edit/{id}"), ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Edit(Customer customer, Address address, Telecom telecom)
        {
            // Gestion du token.
            string accessToken = HttpContext.GetTokenAsync("access_token").Result;
            var repository = APIRepository.Create(accessToken);

            var companyId = ApplicationSettings.CompanyId;
            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(companyId))
            {
                HttpContext.Response.Redirect("/");
                return View();
            }

            customer.Adresse = address;
            customer.Telecom = telecom;

            // [API08] Récupération des comptes généraux dans le formulaire d'édition.
            var accounts = GetActiveAccountsStartingWithNumber(repository, companyId, "41");
            ViewBag.Accounts = accounts["value"];
            ViewBag.Action = "edit";

            // Construction de la requête OData afin d'éditer le client correspondant.
            Dictionary<string, string> options = new Dictionary<string, string>();
            var filter = "id eq ('{customerId}')";
            filter = filter.Replace("{customerId}", customer.Id);
            options.Add("$filter", filter);

            // [API09] Affichage des anciennes valeurs par défaut dans le formulaire d'édition.
            var details = repository.Get(companyId, _resourceName, options).GetJSONResult();
            Customer model = ConvertToCustomer(companyId, details);

            bool customerIsNull = Customer.IsNull(customer);
            bool customerIsEmpty = Customer.IsEmpty(customer);

            if (customerIsNull)
            {
                return View(model);
            }

            // Mise à jour avec les nouvelles valeurs.
            model = customer;

            // Le formulaire a été envoyé.
            if (customerIsEmpty)
            {
                ViewBag.ErrorMessage = "Le numéro, l'intitulé ou le type ne peuvent être des champs vides.";
                return View(model);
            }

            // [API10] Edition des données du client - Envoi des données.
            var accountId = "/comptes('{accountId}')";
            accountId = accountId.Replace("{accountId}", customer.ComptePrincipal);
            customer.Numero = customer.Numero.ToUpper();

            // Liaison odata.bind pour lier le compte au client.
            customer.ComptePrincipal = string.Concat(repository._BASE_URL, companyId, accountId);

            // Conversion en données exploitables.
            var data = JsonConvert.SerializeObject(customer, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            var resourceId = string.Concat(_resourceName, "('{customerId}')");
            resourceId = resourceId.Replace("{customerId}", model.Id);
            var result = repository.Patch(companyId, resourceId, data);

            // Si la mise à jour échoue, on affiche une erreur.
            if (!result.IsSuccessStatusCode)
            {
                var reason = result.ReasonPhrase;
                var explanation = result.Content.ReadAsStringAsync().Result;
                var message = JsonConvert.DeserializeObject<JObject>(explanation);
                var error = string.Concat(message["error"]["code"], " (", reason, ")", " - ", message["error"]["message"].ToString());
                ViewBag.ErrorMessage = error;
            }
            else
            {
                // Redirection.
                return RedirectToRoute(new
                {
                    controller = "Customers",
                    action = "Get",
                    id = model.Id
                });
            }

            return View(model);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="companyId"></param>
        /// <returns></returns>
        private JObject GetActiveAccountsStartingWithNumber(APIRepository repository, string companyId, string numero)
        {
            // Remplissage de la liste déroulante avec les comptes généraux dans le formulaire d'ajout.
            // Nous ne considérons que les comptes actifs (commençant par 41), et un nombre de comptes inférieure à 100, auquel cas nous aurions besoin d'implémenter la pagination du à la limite de requête.
            // Pour plus d'informations à ce sujet:
            // https://developer.sage.com/api/100/fr/saas/ressources/.
            // https://developer.sage.com/api/100/fr/saas/concepts/odatapaginate/

            Dictionary<string, string> accountOptions = new Dictionary<string, string>();
            var select = "id,type,numero";
            var filterAccount = "startswith(numero,'{numero}') and sommeil eq false";
            filterAccount = filterAccount.Replace("{numero}", numero);
            accountOptions.Add("$select", select);
            accountOptions.Add("$filter", filterAccount);
            var accounts = repository.Get(companyId, "comptes", accountOptions).GetJSONResult();

            return accounts;
        }

        /// <summary>
        /// Associe les champs d'un JOBject au modèle Customer.
        /// </summary>
        /// <param name="companyId"> L'id de la société. </param>
        /// <param name="details"> Le JObject a convertir en Customer. </param>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        private Customer ConvertToCustomer(string companyId, JObject details)
        {
            Customer model = new Customer();
            var client = details["value"][0];
            model.Id = client["id"].ToString();
            model.Ape = client["ape"].ToString();
            model.Classement = client["classement"].ToString();
            model.Commentaire = client["commentaire"].ToString();
            model.Contact = client["contact"].ToString();

            model.Identifiant = client["identifiant"].ToString();
            model.Intitule = client["intitule"].ToString();
            model.Numero = client["numero"].ToString();
            model.Qualite = client["qualite"].ToString();
            model.Siret = client["siret"].ToString();
            model.Type = client["type"].ToString();

            string accessToken = HttpContext.GetTokenAsync("access_token").Result;
            var repository = APIRepository.Create(accessToken);

            // Compte principal lié au client.
            var endpoint = "clients('{clientId}')/comptePrincipal";
            endpoint = endpoint.Replace("{clientId}", model.Id);
            var account = repository.Get(companyId, endpoint, new Dictionary<string, string>()).GetJSONResult();
            model.ComptePrincipal = account["numero"].ToString();

            model.Adresse = new Address();
            model.Telecom = new Telecom();

            // Adresse.
            model.Adresse.adresse = client["adresse"]["adresse"].ToString();
            model.Adresse.Pays = client["adresse"]["pays"].ToString();
            model.Adresse.Complement = client["adresse"]["complement"].ToString();
            model.Adresse.CodePostal = client["adresse"]["codePostal"].ToString();
            model.Adresse.CodeRegion = client["adresse"]["codeRegion"].ToString();
            model.Adresse.Ville = client["adresse"]["ville"].ToString();

            // Telecom.
            model.Telecom.Site = client["telecom"]["site"].ToString();
            model.Telecom.EMail = client["telecom"]["eMail"].ToString();
            model.Telecom.Telecopie = client["telecom"]["telecopie"].ToString();
            model.Telecom.Telephone = client["telecom"]["telephone"].ToString();

            return model;
        }
    }
}
