using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using app.Models;
using System.Text;
using app.Repositories;
using Microsoft.AspNetCore.Authentication;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore.Internal;
using System.Globalization;

namespace app.Controllers
{
    public class ImportController : Controller
    {
        public IActionResult Index(IFormFile file)
        {
            // Gestion du token.
            var companyId = HttpContext.Session.GetString("companyId");
            string accessToken = HttpContext.GetTokenAsync("access_token").Result;
            var repository = APIRepository.Create(accessToken);

            if (string.IsNullOrEmpty(accessToken))
            {
                HttpContext.Response.Redirect("/");
                return View();
            }

            // Gestion des paramètres OData pour les futures requêtes.
            var selectParameter = "$select";

            // Récupération des codes journaux, ces codes journaux seront accessibles en les recherchant directement par leurs indices (code).
            Dictionary<string, string> optionsJournaux = new Dictionary<string, string>();
            var select = "code,id";
            optionsJournaux.Add(selectParameter, select);
            var journaux = repository.Get(companyId, "journaux", optionsJournaux).GetJSONResult()["value"];

            // Ces codes journaux seront accessibles par leur clé.
            Dictionary<string, string> codes = new Dictionary<string, string>();
            if (journaux != null)
            {
                foreach (var journal in journaux)
                {
                    var codeJournal = journal["code"].ToString();
                    var idJournal = journal["id"].ToString();
                    codes.Add(codeJournal, idJournal);
                }
            }


            // Récupération des informations de la société afin d'obtenir les dates d'exercices.
            Dictionary<string, string> optionsCompany = new Dictionary<string, string>();
            select = "exercices($select=dateDebut,dateFin;$filter=cloture eq false;$orderby=dateDebut desc;$top=1)";

            optionsCompany.Add(selectParameter, select);
            var company = repository.Get(companyId, "company", optionsCompany).GetJSONResult();
            var exerciceStartDate = (DateTime)company["exercices"][0]["dateDebut"];
            var exerciceEndDate = (DateTime)company["exercices"][0]["dateFin"];

            // Récupération des intitules de modes de réglements.
            Dictionary<string, string> optionsModeReglement = new Dictionary<string, string>();
            select = "intitule,id";
            optionsModeReglement.Add(selectParameter, select);
            var modesReglement = repository.Get(companyId, "modesReglement", optionsModeReglement).GetJSONResult()["value"];
            Dictionary<string, string> intitulesModesReglement = new Dictionary<string, string>();
            foreach (var mode in modesReglement)
            {
                var intituleMode = mode["intitule"].ToString();
                var idMode = mode["id"].ToString();
                intitulesModesReglement.Add(intituleMode, idMode);
            }

            // Traitement.
            var pieces = new Dictionary<string, List<Writing>>();
            var errorsPiece = new Dictionary<string, List<string>>();

            if (file != null)
            {
                List<string> errors = new List<string>();

                // Lecture du fichier.
                using (var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8))
                {

                    var lineNumber = 0;
                    var line = string.Empty;

                    while (!string.IsNullOrEmpty(line = reader.ReadLine()))
                    {
                        lineNumber++;
                        var writing = Writing.Create(line, lineNumber);

                        // Recensement des erreurs relevés.
                        var errorDetails = writing.IsValidWriting(repository, companyId, codes, intitulesModesReglement, exerciceStartDate, exerciceEndDate);
                        errors.AddRange(errorDetails);

                        // Création d'une nouvelle pièce et de ses écritures.
                        if (!pieces.ContainsKey(writing.Id))
                        {
                            pieces.Add(writing.Id, new List<Writing>());
                            errorsPiece.Add(writing.Id, new List<string>());
                        }
                        errorsPiece[writing.Id] = errorsPiece[writing.Id].Concat(errorDetails).ToList();
                        // Ajout de l'écriture à la pièce.
                        pieces[writing.Id].Add(writing);
                    }
                    reader.Close();
                }

                // Test de l'équilibre des pièces et envoi des écritures si valides.
                foreach (var piece in pieces)
                {
                    var totalDebit = Tools.Round(piece.Value.Sum(p => p.Debit));
                    var totalCredit = Tools.Round(piece.Value.Sum(p => p.Credit));
                    if (totalDebit != totalCredit)
                    {
                        var message = string.Concat("Pièce ", piece.Value.First().Piece, ", la pièce n'est pas équilibrée.");
                        errorsPiece[piece.Key].Add(message);
                        errors.Add(message);

                    }
                    if (errorsPiece[piece.Key].Count == 0)
                    {
                        // Requête pour éviter les doublons.
                        Dictionary<string, string> opt = new Dictionary<string, string>();
                        var filter = "piece eq '{noPiece}' and journal/code eq '{code}' and month(date) eq {monthNumber} and year(date) eq {year}";
                        filter = filter.Replace("{noPiece}", piece.Value[0].Piece);
                        filter = filter.Replace("{code}", piece.Value[0].Code);
                        filter = filter.Replace("{monthNumber}", piece.Value[0].Date.Month.ToString());
                        filter = filter.Replace("{year}", piece.Value[0].Date.Year.ToString());
                        opt["$filter"] = filter;

                        // Eviter les doublons.
                        var duplicate = repository.Get(companyId, "ecritures", opt).GetJSONResult();
                        if (duplicate["value"].HasValues)
                        {
                            continue;
                        }
                        foreach (var line in piece.Value)
                        {
                            SetOdataBindings(repository, line, codes, intitulesModesReglement);
                            var data = JsonConvert.SerializeObject(line);
                            data = ((string.IsNullOrEmpty(line.NumeroTiers)) ? data.Replace("\"tiers@odata.bind\":\"\",", "") : data);
                            data = ((string.IsNullOrEmpty(line.ModeReglement)) ? data.Replace("\"modeReglement@odata.bind\":\"\",", "") : data);
                            var result = repository.Post(companyId, "ecritures", data);
                        }
                    }
                }
                // Affichage des erreurs.
                ViewBag.ErrorsPiece = errors;
                var temp = errorsPiece;
            }
            return View();
        }

        /// <summary>
        /// Modifie les propriétés du modèle de sorte à ce que le OData Binding soit effectué.
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="line"> L'écriture comptable à exploiter. </param>
        /// <param name="codes"> Les codes journaux existants. </param>
        /// <param name="modesReglement"> Les modes de réglement connus. </param>
        public void SetOdataBindings(APIRepository repository, Writing line, Dictionary<string, string> codes, Dictionary<string, string> modesReglement)
        {
            var companyId = HttpContext.Session.GetString("companyId");
            var url = string.Concat(repository._BASE_URL, companyId);
            var filterParameter = "$filter";
            var topParameter = "$top";

            // Bind "compte".
            Dictionary<string, string> accountOptions = new Dictionary<string, string>();
            accountOptions[filterParameter] = string.Concat("numero eq '", line.Compte, "'");
            accountOptions[topParameter] = "1";
            var accountResult = repository.Get(companyId, "comptes", accountOptions).GetJSONResult()["value"];
            var accountBind = string.Concat(url, "/comptes('{accountId}')");
            accountBind = accountBind.Replace("{accountId}", accountResult.First()["id"].ToString());
            line.Compte = accountBind;

            // Bind "journal".
            var ledgerBind = string.Concat(url, "/journaux('{journalId}')");
            var memo = codes[line.Code];
            ledgerBind = ledgerBind.Replace("{journalId}", codes[line.Code]);
            line.Code = ledgerBind;

            // Bind "tiers".
            if (!string.IsNullOrEmpty(line.NumeroTiers))
            {
                Dictionary<string, string> tiersOptions = new Dictionary<string, string>();
                accountOptions[filterParameter] = string.Concat("numero eq '", line.NumeroTiers, "'");
                accountOptions[topParameter] = "1";
                var tiersResult = repository.Get(companyId, "tiers", accountOptions).GetJSONResult()["value"];
                var tiersBind = string.Concat(url, "/tiers('{tiersId}')");
                tiersBind = tiersBind.Replace("{tiersId}", tiersResult.First()["id"].ToString());
                line.NumeroTiers = tiersBind;
            }

            // Bind "modeReglement".
            if (!string.IsNullOrEmpty(line.ModeReglement))
            {
                var modeReglementBind = string.Concat(url, "/modesReglement('{modeReglementId}')");
                modeReglementBind = modeReglementBind.Replace("{modeReglementId}", modesReglement[line.ModeReglement]);
                line.ModeReglement = modeReglementBind;
            }
        }
    }
}

