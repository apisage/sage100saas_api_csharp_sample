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
using app.Settings;
using System.Net;

namespace app.Controllers
{
    public class ImportController : Controller
    {
        public IActionResult Index(IFormFile file)
        {
            // Gestion du token.
            string accessToken = HttpContext.GetTokenAsync("access_token").Result;
            var repository = APIRepository.Create(accessToken);
            var companyId = ApplicationSettings.CompanyId;

            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(companyId))
            {
                Error error = new Error(string.Empty, (int)HttpStatusCode.Unauthorized);
                return View("Error", error);
            }

            // Gestion des paramètres OData pour les futures requêtes.
            var selectParameter = "$select";

            // [API11] Import d'écritures - Récupération des données existantes.
            Dictionary<string, string> optionsJournaux = new Dictionary<string, string>();
            var select = "code,id";
            optionsJournaux.Add(selectParameter, select);

            // Récupération des codes journaux, ces codes journaux seront accessibles en les recherchant directement par leurs indices (code).
            var message = repository.Get(companyId, "journaux", optionsJournaux);

            if (!Tools.IsSuccess(message))
            {
                var details = "Désolé ! Les codes journaux existants n'ont pas pu être récupérés !";
                Error error = new Error(details);

                return View("Error", error);
            }

            var journaux = message.GetJSONResult()["value"];

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

            // Récupération des informations de la société afin d'obtenir les dates d'exercices.
            Dictionary<string, string> optionsCompany = new Dictionary<string, string>();
            select = "exercices($select=dateDebut,dateFin;$filter=cloture eq false;$orderby=dateDebut desc;$top=1)";
            optionsCompany.Add(selectParameter, select);
            var company = repository.Get(companyId, "company", optionsCompany).GetJSONResult();
            var exerciceStartDate = (DateTime)company["exercices"][0]["dateDebut"];
            var exerciceEndDate = (DateTime)company["exercices"][0]["dateFin"];

            if (file != null && repository != null && codes != null && intitulesModesReglement != null && exerciceStartDate != null && exerciceEndDate != null)
            {
                Import model = new Import
                {
                    File = file,
                    Codes = codes,
                    ExerciceStartDate = exerciceStartDate,
                    ExerciceEndDate = exerciceEndDate,
                    IntitulesModesReglement = intitulesModesReglement
                };

                return Add(model);
            }
            return View();
        }

        public IActionResult Add(Import existing)
        {
            // Gestion du token.
            string accessToken = HttpContext.GetTokenAsync("access_token").Result;
            var repository = APIRepository.Create(accessToken);
            var companyId = ApplicationSettings.CompanyId;

            if (string.IsNullOrEmpty(accessToken) || null == existing || string.IsNullOrEmpty(companyId))
            {
                Error error = new Error(string.Empty, (int)HttpStatusCode.Unauthorized);
                return View("Error", error);
            }

            // Récupération des données existantes préchargées.
            var file = existing.File;
            var codes = existing.Codes;
            var intitulesModesReglement = existing.IntitulesModesReglement;
            var exerciceStartDate = existing.ExerciceStartDate;
            var exerciceEndDate = existing.ExerciceEndDate;

            // [API12] Import d'écritures - Traitement.
            var pieces = new Dictionary<string, List<Writing>>();
            var errorsPiece = new Dictionary<string, List<string>>();
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

            foreach (var piece in pieces)
            {
                // Test de l'équilibre d'une pièce.
                var totalDebit = Tools.Round(piece.Value.Sum(p => p.Debit));
                var totalCredit = Tools.Round(piece.Value.Sum(p => p.Credit));

                if (totalDebit != totalCredit)
                {
                    var message = string.Concat("Pièce ", piece.Value.First().Piece, ", la pièce n'est pas équilibrée.");
                    errorsPiece[piece.Key].Add(message);
                    errors.Add(message);
                }

                // Les écritures d'une pièce possédant des erreurs de validité ne sera pas ajoutée.
                if (errorsPiece[piece.Key].Count != 0)
                {
                    continue;
                }

                // Requête permettant de détecter s'il y a déjà une écriture similaire (pour éviter les doublons).
                Dictionary<string, string> optionsDuplicate = new Dictionary<string, string>();
                var pieceFilter = piece.Value[0].Piece;
                var codeFilter = piece.Value[0].Code;
                var monthFilter = piece.Value[0].Date.Month.ToString();
                var yearFilter = piece.Value[0].Date.Year.ToString();
                var filter = string.Format("piece eq '{0}' and journal/code eq '{1}' and month(date) eq {2} and year(date) eq {3}", pieceFilter, codeFilter, monthFilter, yearFilter);
                optionsDuplicate["$filter"] = filter;

                // Quand on détecte un doublon, les écritures de la pièce courante ne seront pas ajoutées.
                var duplicate = repository.Get(companyId, "ecritures", optionsDuplicate).GetJSONResult();

                if (duplicate["value"].HasValues)
                {
                    continue;
                }

                foreach (var line in piece.Value)
                {
                    SetOdataBindings(repository, companyId, line, codes, intitulesModesReglement);
                    var data = JsonConvert.SerializeObject(line);
                    data = ((string.IsNullOrEmpty(line.NumeroTiers)) ? data.Replace("\"tiers@odata.bind\":\"\",", string.Empty) : data);
                    data = ((string.IsNullOrEmpty(line.ModeReglement)) ? data.Replace("\"modeReglement@odata.bind\":\"\",", string.Empty) : data);
                    var message = repository.Post(companyId, "ecritures", data);
                }
            }
            // Affichage des erreurs.
            ViewBag.ErrorsPiece = errors;

            return View();
        }

        /// <summary>
        /// Modifie les propriétés du modèle de sorte à ce que le OData Binding soit effectué.
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="line"> L'écriture comptable à exploiter. </param>
        /// <param name="codes"> Les codes journaux existants. </param>
        /// <param name="modesReglement"> Les modes de réglement connus. </param>
        public void SetOdataBindings(APIRepository repository, string companyId, Writing line, Dictionary<string, string> codes, Dictionary<string, string> modesReglement)
        {
            var url = string.Concat(repository._BASE_URL, companyId);
            var filterParameter = "$filter";
            var topParameter = "$top";

            // Bind "compte": Recherche de l'id du numéro de compte inscrit dans le fichier.
            Dictionary<string, string> accountOptions = new Dictionary<string, string>
            {
                [filterParameter] = string.Concat("numero eq '", line.Compte, "'"),
                [topParameter] = "1"
            };

            var message = repository.Get(companyId, "comptes", accountOptions);
            var accountId = message.GetJSONResult()["value"][0]["id"].ToString();
            var accountBind = string.Concat(url, "/comptes('{accountId}')");
            accountBind = accountBind.Replace("{accountId}", accountId);
            line.Compte = accountBind;

            // Bind id "tiers": Recherche du numero.
            if (!string.IsNullOrEmpty(line.NumeroTiers))
            {
                var filter = "numero eq '{numeroTiers}'";
                filter = filter.Replace("{numeroTiers}", line.NumeroTiers);
                Dictionary<string, string> tiersOptions = new Dictionary<string, string>
                {
                    [filterParameter] = filter,
                    [topParameter] = "1"
                };

                message = repository.Get(companyId, "tiers", tiersOptions);
                var tiersId = message.GetJSONResult()["value"][0]["id"].ToString();
                var tiersBind = string.Concat(url, "/tiers('{tiersId}')");
                tiersBind = tiersBind.Replace("{tiersId}", tiersId);
                line.NumeroTiers = tiersBind;
            }

            // Bind code "journal" si il existe.
            var journalBind = string.Concat(url, "/journaux('{journalId}')");
            var journalId = codes[line.Code];
            journalBind = journalBind.Replace("{journalId}", journalId);
            line.Code = journalBind;

            // Bind id de "modeReglement" si il existe.
            if (!string.IsNullOrEmpty(line.ModeReglement))
            {
                var modeReglementIdBind = string.Concat(url, "/modesReglement('{modeReglementId}')");
                var modeReglementId = modesReglement[line.ModeReglement];
                modeReglementIdBind = modeReglementIdBind.Replace("{modeReglementId}", modeReglementId);
                line.ModeReglement = modeReglementIdBind;
            }
        }
    }
}

