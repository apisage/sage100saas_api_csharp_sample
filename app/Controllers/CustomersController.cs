using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using app.Models;
using app.Repositories;
using Microsoft.AspNetCore.Authentication;
using Newtonsoft.Json.Serialization;
using app.Settings;
using System.Globalization;
using System.Net.Http;

namespace app.Controllers
{
    public class CustomersController : Controller
    {

        //****************************[LISTE DES CLIENTS]*******************************************************************
        /// <summary>
        /// Affiche liste des clients éventuellement filtrée par un terme de recherche.
        /// </summary>
        /// <returns> La liste des clients </returns>
        public IActionResult Index(string searchTerm = "", string skip = "0", string top = "10", string DefaultSorting="numero",string SortingDesc="")
        {
            //Contrôle token et company et création d'un repository
            var repository = APIRepository.Create(HttpContext.GetTokenAsync("access_token").Result, ApplicationSettings.CompanyId);
            if (repository.ErrorCode == "TOKENEXPIRED") return RedirectToRoute(Tools.forceAuthentication);
            if (repository.ErrorMessage != "") return View("Error", new Error(repository.ErrorMessage));

            // Construction de la requête de recherche OData.
            // [API04] Récupération des clients selon le champ de recherche.
            var qryOrderBy = (SortingDesc != "Y")?DefaultSorting: String.Join(" desc,", DefaultSorting.Split(",")) + " desc";
            var qrySkip = skip ?? "0";
            var qryFilter = (string.IsNullOrEmpty(searchTerm)) ? "" : "contains(tolower(intitule), '"+ searchTerm.ToLower() + "') or contains(tolower(numero), '"+ searchTerm.ToLower() + "')";
 
            Dictionary<string, string> options = new Dictionary<string, string>();

            // Récupération du nombre total d'enregistrements répondant au critère de filtre
            options.Clear();
            options.Add("$count", "true");
            options.Add("$top", "0");
            options.Add("$filter", qryFilter);
            var result = repository.Get(repository.CompanyId, "clients", options);
            if (!Tools.IsSuccess(result)) 
                return View("Error", ApplicationSettings.ApiError);         
            var count = result.GetJSONResult()["@odata.count"].ToString();

            // Récupération d'un lot d'enregistrements
            options.Clear();
            options.Add("$orderby", qryOrderBy);
            options.Add("$select", "id,intitule,numero,adresse,telecom/telephone");
            options.Add("$filter", qryFilter);
            options.Add("$top", top);
            options.Add("$skip", qrySkip);  
            result = repository.Get(repository.CompanyId, "clients", options);
            if (!Tools.IsSuccess(result)) 
                return View("Error", ApplicationSettings.ApiError);

            //Affectation des propriétés du ViewBag
            ViewBag.Results = Int32.Parse(count);
            ViewBag.ResultsByPage = Int32.Parse(top);
            ViewBag.Clients = result.GetJSONResult();
            ViewBag.SearchTerm = searchTerm;
            ViewBag.Options = options;
            ViewBag.CurrentPage = (qrySkip != "0") ? (Int32.Parse(qrySkip)) : 1;
            ViewBag.Columns = ColumnsListCustomer.LoadColumns();
            ViewBag.DefaultSorting = DefaultSorting;
            ViewBag.SortingDesc = SortingDesc;

            return View();
        }

        //****************************[AJAX CALCUL DU SOLDE D'UN LOT DE CLIENTS]*******************************************************************
        // Traitement Ajax calcule et renvoie le solde de la liste des numeros clients séparés par un |
        // <Request.Form>les numéros de client séparés par un |
        // <returns> un string sous la forme numero~solde|numero~solde|... </returns>
        [HttpPost("Customers/AjaxCalculSolde")]
        public  ContentResult AjaxCalculSolde()
        {
            //Contrôle token et company et création d'un repository
            var repository = APIRepository.Create(HttpContext.GetTokenAsync("access_token").Result, ApplicationSettings.CompanyId,true);
 			if (repository.ErrorMessage != "") return Content("ERROR:AjaxCalculSolde : "+repository.ErrorMessage);
            
		    string retour = "";
            HttpResponseMessage result;
            // Récupération du solde pour chaque client dont le numéro est mentionné dans numeros   
            try
            {
                foreach (string id in HttpContext.Request.Form.Keys)
                {
                    result = repository.Get(repository.CompanyId, "clients", null, id, "solde()");
                    if (!Tools.IsSuccess(result))
                        return Content("ERROR:AjaxCalculSolde : " + Tools.FormateErrorApi(result));
                    var solde = Convert.ToDecimal(result.GetJSONResult()["value"].ToString()).ToString("C2", new CultureInfo("fr-FR"));
                    retour += ((retour != "") ? "|" : "") + id + "~" + solde;
                }
            }
            catch(Microsoft.AspNetCore.Server.Kestrel.Core.BadHttpRequestException)
            {
                retour = "ERROR:AjaxCalculSolde : 'Unexpected end of request content";
            }
        
            return Content(retour);
        }

        //****************************[FORMULAIRE NOUVEAU CLIENT]*******************************************************************
        /// <summary>
        /// Affiche et gère la formulaire d'ajout de Customer.
        /// </summary>
        /// <returns> La vue avec le nouveau Customer. </returns>
        [HttpGet("Customers/add")]
        public IActionResult Add()
        {
            ViewBag.Action = "add";

            //Contrôle token et company et création d'un repository
            var repository = APIRepository.Create(HttpContext.GetTokenAsync("access_token").Result, ApplicationSettings.CompanyId);
            if (repository.ErrorCode == "TOKENEXPIRED") return RedirectToRoute(Tools.forceAuthentication);
            if (repository.ErrorMessage != "") return View("Error", new Error(repository.ErrorMessage));

            // [API06] Récupération des paramètres
            LoadParameters(repository);

            Customer customer = new Customer
            {
                Adresse = new Address(),
                Telecom = new Telecom()
            };

            return View(customer);
        }

        //****************************[VALIDATION NOUVEAU CLIENT]*******************************************************************
        [HttpPost("Customers/add")]
        public IActionResult Add(Customer customer, Address address, Telecom telecom)
        {
            ViewBag.Action = "add";

            //Contrôle token et company et création d'un repository
            var repository = APIRepository.Create(HttpContext.GetTokenAsync("access_token").Result, ApplicationSettings.CompanyId);
            if (repository.ErrorCode == "TOKENEXPIRED") return RedirectToRoute(Tools.forceAuthentication);
            if (repository.ErrorMessage != "") return View("Error", new Error(repository.ErrorMessage));

            customer.Adresse = address;
            customer.Telecom = telecom;
            customer.Numero = customer.Numero.ToUpper();

            // [API06] Récupération des paramètres
            LoadParameters(repository);

            // Contrôle sécurité champs obligatoires non vides (Mais par défaut le formulaire empêche déjà de valider si champs obligatoires vides)
            if (Customer.IsEmpty(customer))
            {
                ViewBag.ErrorMessage = Resource.CLIENT_MANDATORY_FIELDS;
                return View(customer);
            }

            // Il ne peut exister deux clients avec le même compte tiers (numero).
            var tiersExist = ControlTiersAlreadyExist(customer.Numero, repository);
            if (tiersExist!="")
            {
                ViewBag.ErrorMessage = tiersExist;
                return View(customer);
            }
            
            // [API07] Ajout des données du client - Envoi des données.
            // Liaison du compte principal au client à ajouter.
            customer.ComptePrincipal = Tools.OdataBind(repository, "comptes", customer.ComptePrincipal);

            // Conversion en données exploitables.
            var data = JsonConvert.SerializeObject(customer, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            // Envoi des données pour l'ajout.
            var result = repository.Post(repository.CompanyId, "clients", data);

            // Si ajout refusé par l'API
            if (!result.IsSuccessStatusCode)
            {
                ViewBag.ErrorMessage = Tools.FormateErrorApi(result);
                return View(customer);
            }
            else
            {
                // Redirection.
                return RedirectToRoute(new
                {
                    controller = "Customers",
                    action = "Get",
                    id = result.GetJSONResult()["id"].ToString(),
                    refresh = Guid.NewGuid().ToString()
                }) ;
            }
        }

        //****************************[DETAIL CLIENT]*******************************************************************
        /// <summary>
        /// Affiche les informations d'un client.
        /// </summary>
        /// <param name="id"> Id du client dont on souhaite consulter les informations. </param>
        /// <returns> La vue du client. </returns>
        [HttpGet("Customers/show/{id}")]
        public IActionResult Get(string id)
        {
            //Contrôle token et company et création d'un repository
            var repository = APIRepository.Create(HttpContext.GetTokenAsync("access_token").Result, ApplicationSettings.CompanyId);
            if (repository.ErrorCode == "TOKENEXPIRED") return RedirectToRoute(Tools.forceAuthentication);
            if (repository.ErrorMessage != "") return View("Error", new Error(repository.ErrorMessage));

            // [API05] Consultation d'une fiche client.
            var customer = GetCustomerById(id, repository);
            if (Customer.IsNull(customer) || Customer.IsEmpty(customer))
                return View("Error", new Error(Resource.CLIENT_NOTFOUND));          

            return View(customer);
        }

        //****************************[AJAX CALCUL SOLDE et DATES D'UN CLIENT]*******************************************************************
        // Traitement Ajax calcule et renvoie le solde et les dates dernière facture et règlement d'un client
        // <Request.Form>l'id du client
        // <returns> un string sous la forme solde|datefacture|datereglement </returns>
        [HttpPost("Customers/AjaxCalculSoldeEtDates")]
        public ContentResult AjaxCalculSoldeEtDates()
        {
            //Contrôle token et company et création d'un repository
            var repository = APIRepository.Create(HttpContext.GetTokenAsync("access_token").Result, ApplicationSettings.CompanyId, true);
            if (repository.ErrorMessage != "") return Content("ERROR:AjaxCalculSoldeEtDates : " + repository.ErrorMessage);

            string retour, value;
            HttpResponseMessage result;
            string id = HttpContext.Request.Form["Id"];

            // Récupération du solde et des dates pour le client courant 
            try
            {
                result = repository.Get(repository.CompanyId, "clients", null, id, "solde()");
                if (!Tools.IsSuccess(result))
                    return Content("ERROR:AjaxCalculSoldeEtDates Solde : " + Tools.FormateErrorApi(result));
                value = result.GetJSONResult()["value"].ToString();
                retour = Convert.ToDecimal(result.GetJSONResult()["value"].ToString()).ToString("C2", new CultureInfo("fr-FR"));

                result = repository.Get(repository.CompanyId, "clients", null, id, "derniereFacture()");
                if (!Tools.IsSuccess(result))
                    return Content("ERROR:AjaxCalculSoldeEtDates DateFacture : " + Tools.FormateErrorApi(result));
                value = result.GetJSONResult()["value"].ToString();
                retour += "|" + ((Convert.ToDateTime(value) == DateTime.MinValue) ? "": Convert.ToDateTime(value).ToString("d", new CultureInfo("fr-FR")));

                result = repository.Get(repository.CompanyId, "clients", null, id, "dernierReglement()");
                if (!Tools.IsSuccess(result))
                    return Content("ERROR:AjaxCalculSoldeEtDates DateReglement : " + Tools.FormateErrorApi(result));
                value = result.GetJSONResult()["value"].ToString();
                retour += "|" + ((Convert.ToDateTime(value) == DateTime.MinValue) ? "" : Convert.ToDateTime(value).ToString("d", new CultureInfo("fr-FR")));
            }
            catch (Microsoft.AspNetCore.Server.Kestrel.Core.BadHttpRequestException)
            {
                retour = "ERROR:AjaxCalculSoldeEtDates : 'Unexpected end of request content";
            }

            return Content(retour);
        }

        //****************************[AJAX TEST DOUBLON COMPTE TIERS]*******************************************************************
        /// <summary>
        /// Traitement Ajax contrôle si numéro nouveau client existe déjà dans l'ensemble des tiers
        /// </summary>
        /// <param name="numero"> Le numéro du nouveau client saisi dans le formulaire </param>
        /// <returns> un texte d'avertissement si le numéro tiers existe déjà </returns>
        [HttpGet("Customers/AjaxCheckClientNumber/{numero}")]
        public ContentResult CheckClientNumber(string numero)
        {
            //Contrôle token et company et création d'un repository
            var repository = APIRepository.Create(HttpContext.GetTokenAsync("access_token").Result, ApplicationSettings.CompanyId);
            if (repository.ErrorMessage != "") return Content("ERROR:AjaxCalculSolde : " + repository.ErrorMessage);

            return Content(ControlTiersAlreadyExist(numero,repository));
        }

        //****************************[FORMULAIRE MODIFICATION CLIENT]*******************************************************************
        /// <summary>
        /// Affiche la vue du formulaire d'édition.
        /// </summary>
        /// <param name="id"> Le client à éditer. </param>
        /// <returns> La vue du formulaire d'édition du client. </returns>
        [HttpGet("Customers/edit/{id}")]
        public IActionResult Edit(string id)
        {
            ViewBag.Action = "edit";

            //Contrôle token et company et création d'un repository
            var repository = APIRepository.Create(HttpContext.GetTokenAsync("access_token").Result, ApplicationSettings.CompanyId);
            if (repository.ErrorCode == "TOKENEXPIRED") return RedirectToRoute(Tools.forceAuthentication);
            if (repository.ErrorMessage != "") return View("Error", new Error(repository.ErrorMessage));

            // [API06] Récupération des paramètres
            LoadParameters(repository);

            Customer customer = GetCustomerById(id, repository);
            if (Customer.IsNull(customer) || Customer.IsEmpty(customer))
                return View("Error", new Error(Resource.CLIENT_NOTFOUND));

            return View(customer);
        }

        //****************************[VALIDATION MODIFICATION CLIENT]*******************************************************************
        /// <summary>
        /// Mise à jour d'un client.
        /// </summary>
        /// <param name="customer"> Contient les nouvelles valeurs pour chaque propriété non objet. </param>
        /// <param name="address"> Contient les nouvelles valeurs pour l'objet adresse.</param>
        /// <param name="telecom"> Contient les nouvelles valeurs pour l'objet telecom.</param>
        /// <returns> La vue de la fiche client. </returns>
        [HttpPost("Customers/edit/{id}")]
        public IActionResult Edit(Customer customer, Address address, Telecom telecom, string id)
        {
            ViewBag.Action = "edit";

            //Contrôle token et company et création d'un repository
            var repository = APIRepository.Create(HttpContext.GetTokenAsync("access_token").Result, ApplicationSettings.CompanyId);
            if (repository.ErrorCode == "TOKENEXPIRED") return RedirectToRoute(Tools.forceAuthentication);
            if (repository.ErrorMessage != "") return View("Error", new Error(repository.ErrorMessage));

            customer.Adresse = address;
            customer.Telecom = telecom;
            customer.Numero = customer.Numero.ToUpper();

            // [API06] Récupération des paramètres
            LoadParameters(repository);

            if (Customer.IsEmpty(customer))
            {
                ViewBag.ErrorMessage = Resource.CLIENT_MANDATORY_FIELDS;
                return View(customer);
            }

            // [API10] Edition des données du client - Envoi des données.
            customer.ComptePrincipal = Tools.OdataBind(repository, "comptes", customer.ComptePrincipal);

            // Conversion en données exploitables.
            var data = JsonConvert.SerializeObject(customer, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            var result = repository.Patch(repository.CompanyId, "clients", data, id);

            // Si la mise à jour échoue, on affiche une erreur.
            if (!result.IsSuccessStatusCode)
            {
                ViewBag.ErrorMessage= Tools.FormateErrorApi(result);
                return View(customer);
            }
            else
            {
                // Redirection.
                return RedirectToRoute(new
                {
                    controller = "Customers",
                    action = "Get",
                    id = customer.Id,
                    refresh = Guid.NewGuid().ToString()
                });
            }
        }

        //****************************[SUPPRESSION CLIENT]*******************************************************************
        /// <summary>
        /// Supprime un client.
        /// </summary>
        /// <param name="id"> L'id du client. </param>
        /// <returns></returns>
        [Route("Customers/delete/{id}")]
        public IActionResult Delete(string id)
        {
            //Contrôle token et company et création d'un repository
            var repository = APIRepository.Create(HttpContext.GetTokenAsync("access_token").Result, ApplicationSettings.CompanyId);
            if (repository.ErrorCode == "TOKENEXPIRED") return RedirectToRoute(Tools.forceAuthentication);
            if (repository.ErrorMessage != "") return View("Error", new Error(repository.ErrorMessage));

            repository.Delete(repository.CompanyId, "clients", id);

            // Redirection.
            return RedirectToRoute(new
            {
                controller = "Customers",
                action = "Index"
            });
        }

        /// <summary>
        /// Récupère un objet Customer selon un id donné.
        /// </summary>
        /// <param name="id"> L'id du client à récupérer. </param>
        /// <param name="repository"> Le repository. </param>
        /// <returns>Un objet Customer.</returns>
        public Customer GetCustomerById(string id, APIRepository repository)
        {
            // Construction de la requête OData afin de récupérer le client correspondant.
            Dictionary<string, string> options = new Dictionary<string, string>();
            var expand = "comptePrincipal($select=id,numero)";
            options.Add("$expand", expand);
            var result = repository.Get(repository.CompanyId, "clients", options, id);

            if (!Tools.IsSuccess(result))
                return new Customer(new Address(), new Telecom());

            Customer customer = Tools.ConvertToCustomer(result.GetJSONResult());
            return customer;
        }

        /// <summary>
        /// Récupération des paramètres (type comptes généraux, pays). Alimente les propriétés de ViewBag.
        /// </summary>
        /// <param name="repository">Repository courant</param>
        public void LoadParameters(APIRepository repository)
        {
            Dictionary<string, string> options = new Dictionary<string, string>();

            //Liste des comptes généraux commençant par 41
            options.Clear();
            options.Add("$select", "id,type,numero");
            options.Add("$filter", "startswith(numero,'41') and sommeil eq false");
            options.Add("$orderby", "numero");
            ViewBag.Accounts = repository.Get(repository.CompanyId, "comptes", options).GetJSONResult()["value"];

            //Liste des pays
            options.Clear();
            options.Add("$select", "intitule");
            options.Add("$orderby", "intitule");
            ViewBag.Pays = repository.Get(repository.CompanyId, "pays", options).GetJSONResult()["value"];
        }

        /// <summary>
        /// Controle si le numero de Tiers saisi existe et si oui retourne un message d'erreur
        /// </summary>
        /// <param name="numero"> Le compte tiers à tester </param>
        /// <param name="repository">Le repository </param>
        /// <returns>Un string avec le message formaté si le tiers existe déjà ou une chaine vide si inexistant</returns>
        private string ControlTiersAlreadyExist(string numero, APIRepository repository)
        {
            Dictionary<string, string> options = new Dictionary<string, string>();
            options.Add("$filter", "numero eq '" + numero.ToUpper() + "'");
            options.Add("$select", "numero,intitule,adresse,type");

            var result = repository.Get(repository.CompanyId, "tiers", options);
            if (!Tools.IsSuccess(result))
                return "ERROR:" + Tools.FormateErrorApi(result);

            var values = result.GetJSONResult()["value"];
            if (!values.HasValues) return "";
            return Resource.CLIENT_ALREADY_EXIST.
                Replace("{numero}", "<b>"+values[0]["numero"].ToString()+"</b>").
                Replace("{intitule}","<b>"+ values[0]["intitule"].ToString()+"</b>").
                Replace("{type}", "<b>"+values[0]["type"].ToString()+"</b>");
        }
    }
}