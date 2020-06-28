using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using app.Models;
using app.Repositories;
using System.Reflection;
using Microsoft.AspNetCore.Authentication;

namespace app.Controllers
{
    public class CustomersController : Controller
    {
        private readonly string _resourceName = "clients";

        /// <summary>s
        /// Affiche la vue principale Customers.
        /// </summary>
        /// <returns> La vue Customers. </returns>
        public IActionResult Index(string searchTerm = "", string skip = "0")
        {
            // Construction de la requête de recherche OData.
            Dictionary<string, string> options = new Dictionary<string, string>();
            var qry_count = "true";
            var qry_skip = skip ?? "0";
            var qry_filter = (string.IsNullOrEmpty(searchTerm)) ?null: "contains(tolower(intitule), '" + searchTerm.ToLower() + "') or contains(tolower(numero), '" + searchTerm.ToLower() + "')";
            var qry_orderby = "intitule,numero";
            options.Add("$count", qry_count);
            options.Add("$skip", qry_skip);
            if (qry_filter != null) options.Add("$filter", qry_filter);
            options.Add("$orderby", qry_orderby);
            options.Add("$top", "10");

            // Token.
            string accessToken = HttpContext.GetTokenAsync("access_token").Result;
            var repository = APIRepository.Create(accessToken);
            if (string.IsNullOrEmpty(accessToken))
            {
                HttpContext.Response.Redirect("/");
                return View();
            }

            // Récupération des clients.
            ViewBag.Clients = repository.Get(HttpContext.Session.GetString("companyId"), _resourceName, options).GetJSONResult();
            ViewBag.SearchTerm = searchTerm;
            ViewBag.options = options;
            var page = Int32.Parse(qry_skip);
            ViewBag.CurrentPage = (page != 0) ? (page) : 1;
            return View();
        }

        /// <summary>
        /// Affiche et gère la formulaire d'ajout de Customer.
        /// </summary>
        /// <returns> La vue avec le nouveau Customer. </returns>
        [Route("Customers/add")]
        public IActionResult Add(Customer customer, Customer.Adresse address, Customer.Telecom telecom)
        {
            // Remplissage de la liste déroulante avec les comptes généraux dans le formulaire d'ajout.
            // Nous ne considérons que les comptes actifs (commençant par 41), et un nombre de comptes inférieure à 100, auquel cas nous aurions besoin d'implémenter la pagination du à la limite de requête.
            // Pour plus d'informations à ce sujet:
            // https://developer.sage.com/api/100/fr/saas/ressources/.
            // https://developer.sage.com/api/100/fr/saas/concepts/odatapaginate/
            Dictionary<string, string> options = new Dictionary<string, string>();
            var count = "true";
            var select = "id,type,numero";
            var filter = "startswith(numero,'41') and sommeil eq false";
            options.Add("$count", count);
            options.Add("$select", select);
            options.Add("$filter", filter);

            ViewBag.Action = "add";

            // Gestion du token.
            string accessToken = HttpContext.GetTokenAsync("access_token").Result;
            var repository = APIRepository.Create(accessToken);
            var accounts = repository.Get(HttpContext.Session.GetString("companyId"), "comptes", options).GetJSONResult();
            ViewBag.Accounts = accounts["value"];

            Customer model = new Customer();
            model.adresse = new Customer.Adresse();
            model.telecom = new Customer.Telecom();

            if (!string.IsNullOrEmpty(customer.numero))
            {
                // Lie les données adresse et telecom au client.
                customer.adresse = address;
                customer.telecom = telecom;

                // Récupération des propriétés du type Customer de sorte à ce qu'elles soient vides par défaut au lieu de null.
                foreach (PropertyInfo prop in typeof(Customer).GetProperties())
                {
                    if (prop.GetValue(model) == null && prop.PropertyType.Equals("System.String"))
                    {
                        prop.SetValue(model, string.Empty);
                    }
                }

                // Liaison du compte principal au client à ajouter.
                var accountId = "/comptes('{accountId}')";
                accountId = accountId.Replace("{accountId}", customer.comptePrincipal);
                customer.comptePrincipal = string.Concat(repository._BASE_URL, HttpContext.Session.GetString("companyId"), accountId);

                // Conversion de Customer en données exploitables par la méthode POST.
                var data = JsonConvert.SerializeObject(customer);

                if (!string.IsNullOrEmpty(customer.intitule) || !string.IsNullOrEmpty(customer.numero) || !string.IsNullOrEmpty(customer.type))
                {
                    // Envoi des données.
                    var result = repository.Post(HttpContext.Session.GetString("companyId"), _resourceName, data);

                    // Gestion d'erreurs.
                    if (!result.IsSuccessStatusCode)
                    {
                        model = customer;
                        var reason = result.ReasonPhrase;
                        var explanation = result.Content.ReadAsStringAsync().Result;
                        var message = JsonConvert.DeserializeObject<JObject>(explanation);
                        var error = string.Concat(message["error"]["code"], " (", reason, ")", " - ", message["error"]["message"].ToString());
                        HttpContext.Response.HttpContext.Session.SetString("error", error);
                    }
                    else
                    {
                        // Redirection.
                        var detailsPage = "/Customers";
                        HttpContext.Response.Redirect(detailsPage);
                    }
                }
            }
            return View(model);
        }

        /// <summary>
        /// Affiche les informations d'un client.
        /// </summary>
        /// <param name="id"> Id du client dont on souhaite consulter les informations. </param>
        /// <returns> La vue du cient. </returns>
        [Route("Customers/show/{id}")]
        public IActionResult Get(string id)
        {
            // Construction de la requête OData afin d'obtenir une requête précise.
            Dictionary<string, string> options = new Dictionary<string, string>();
            var count = "true";
            var filter = "id eq ('" + id + "')";
            options.Add("$count", count);
            options.Add("$filter", filter);

            // Gestion du token.
            string accessToken = HttpContext.GetTokenAsync("access_token").Result;
            var repository = APIRepository.Create(accessToken);

            // Récupération des données et liaison avec le modèle.
            var details = repository.Get(HttpContext.Session.GetString("companyId"), _resourceName, options).GetJSONResult();
            Customer model = MapToCustomer(details);
            return View(model);
        }

        /// <summary>
        /// Mise à jour d'un client.
        /// </summary>
        /// <param name="pCustomer"> Contient les nouvelles valeurs pour chaque propriété non objet. </param>
        /// <param name="pAddress"> Contient les nouvelles valeurs pour l'objet adresse.</param>
        /// <param name="pTelecom"> Contient les nouvelles valeurs pour l'objet telecom.</param>
        /// <returns> The view of a customer. </returns>
        [Route("Customers/edit/{id}")]
        public IActionResult Edit(Customer customer, Customer.Adresse address, Customer.Telecom telecom)
        {
            // Récupération et affichages des comptes dans la liste déroulante du formulaire d'édition de client.
            Dictionary<string, string> accountOptions = new Dictionary<string, string>();
            var count = "true";
            var select = "id,type,numero";
            var filterAccount = "startswith(numero,'41') and sommeil eq false";
            accountOptions.Add("$count", count);
            accountOptions.Add("$select", select);
            accountOptions.Add("$filter", filterAccount);

            ViewBag.Action = "edit";

            string accessToken = HttpContext.GetTokenAsync("access_token").Result;

            //On a aussi accès au refresh_token
            //string refreshToken = await HttpContext.GetTokenAsync("refresh_token");
            //}
            var repository = APIRepository.Create(accessToken);
            var accounts = repository.Get(HttpContext.Session.GetString("companyId"), "comptes", accountOptions).GetJSONResult();
            ViewBag.Accounts = accounts["value"];

            customer.adresse = address;
            customer.telecom = telecom;

            // Construction de la requête OData afin d'éditer le client correspondant.
            Dictionary<string, string> options = new Dictionary<string, string>();
            var filter = "id eq ('{customerId}')";
            filter = filter.Replace("{customerId}", customer.id);
            options.Add("$count", count);
            options.Add("$filter", filter);

            // Le formulaire d'édition affichera les anciennes valeurs par défaut.
            var details = repository.Get(HttpContext.Session.GetString("companyId"), _resourceName, options).GetJSONResult();
            Customer model = MapToCustomer(details);

            // Mise à jour avec les nouvelles valeurs.
            if (!string.IsNullOrEmpty(customer.intitule) || !string.IsNullOrEmpty(customer.numero) || !string.IsNullOrEmpty(customer.type))
            {
                // Nouvelles valeurs.
                model = customer;

                // Envoie d'une adresse pour lier le compte au client.
                var accountId = "/comptes('{accountId}')";
                accountId = accountId.Replace("{accountId}", customer.comptePrincipal);
                customer.comptePrincipal = string.Concat(repository._BASE_URL, HttpContext.Session.GetString("companyId"), accountId);

                // Conversion en données exploitables.
                var data = JsonConvert.SerializeObject(customer);
                var resourceId = _resourceName + "('{customerId}')";
                resourceId = resourceId.Replace("{customerId}", model.id);
                var result = repository.Patch(HttpContext.Session.GetString("companyId"), resourceId, data);

                // Si la mise à jour échoue, on affiche une erreur.
                if (!result.IsSuccessStatusCode)
                {
                    model = customer;
                    var reason = result.ReasonPhrase;
                    var explanation = result.Content.ReadAsStringAsync().Result;
                    var message = JsonConvert.DeserializeObject<JObject>(explanation);
                    var error = string.Concat(message["error"]["code"], " (", reason, ")", " - ", message["error"]["message"].ToString());
                    HttpContext.Response.HttpContext.Session.SetString("error", error);
                }
                else
                {
                    // Redirection.
                    var detailsPage = "/Customers/show/{customerId}";
                    detailsPage = detailsPage.Replace("{customerId}", model.id);
                    HttpContext.Response.Redirect(detailsPage);
                }
            }
            return View(model);
        }

        /// <summary>
        /// Associe les champs d'un JOBject au modèle Customer.
        /// </summary>
        /// <param name="details"></param>
        private Customer MapToCustomer(JObject details)
        {
            Customer model = new Customer();
            var client = details["value"][0];
            model.id = client["id"].ToString();
            model.ape = client["ape"].ToString();
            model.classement = client["classement"].ToString();
            model.commentaire = client["commentaire"].ToString();
            model.contact = client["contact"].ToString();

            model.identifiant = client["identifiant"].ToString();
            model.intitule = client["intitule"].ToString();
            model.numero = client["numero"].ToString();
            model.qualite = client["qualite"].ToString();
            model.siret = client["siret"].ToString();
            model.type = client["type"].ToString();

            string accessToken = HttpContext.GetTokenAsync("access_token").Result;

            //On a aussi accès au refresh_token
            //string refreshToken = await HttpContext.GetTokenAsync("refresh_token");
            //}
            var repository = APIRepository.Create(accessToken);


            // Compte principal lié au client.
            var endpoint = "clients('{clientId}')/comptePrincipal";
            endpoint = endpoint.Replace("{clientId}", model.id);
            var account = repository.Get(HttpContext.Session.GetString("companyId"), endpoint, new Dictionary<string, string>()).GetJSONResult();
            model.comptePrincipal = account["numero"].ToString();

            model.adresse = new Customer.Adresse();
            model.telecom = new Customer.Telecom();

            // Adresse.
            model.adresse.adresse = client["adresse"]["adresse"].ToString();
            model.adresse.pays = client["adresse"]["pays"].ToString();
            model.adresse.complement = client["adresse"]["complement"].ToString();
            model.adresse.codePostal = client["adresse"]["codePostal"].ToString();
            model.adresse.codeRegion = client["adresse"]["codeRegion"].ToString();
            model.adresse.ville = client["adresse"]["ville"].ToString();

            // Telecom.
            model.telecom.site = client["telecom"]["site"].ToString();
            model.telecom.eMail = client["telecom"]["eMail"].ToString();
            model.telecom.telecopie = client["telecom"]["telecopie"].ToString();
            model.telecom.telephone = client["telecom"]["telephone"].ToString();

            return model;
        }
    }
}
