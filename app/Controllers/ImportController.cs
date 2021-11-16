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

namespace app.Controllers
{
    public class ImportController : Controller
    {
        //****************************[AFFICHAGE PAGE ACCUEIL IMPORT]*******************************************************************
        public IActionResult Index()
        {
            //Contrôle token et company et création d'un repository
            var repository = APIRepository.Create(HttpContext.GetTokenAsync("access_token").Result, ApplicationSettings.CompanyId);
            if (repository.ErrorCode == "TOKENEXPIRED") return RedirectToRoute(Tools.forceAuthentication);
            if (repository.ErrorMessage != "") return View("Error", new Error(repository.ErrorMessage));

            return View();
        }

        //****************************[ETAPE DETAIL CONSULTÉ]*******************************************************************
        [HttpPost("Detail")]
        public IActionResult Detail()
        {
            ViewBag.nextStep = 2;
            return View("Index");
        }

 
        //****************************[ETAPE CONTROLE CONTENU FICHIER IMPORT]*******************************************************************
        /// <summary>
        /// Lit le fichier et ajoute les écritures correctes.
        /// </summary>
        /// <param name="file"> Le fichier importé. </param>
        /// <param name="historicalData"> Les données existantes permettant à convertir en objet Import. </param>
        /// <returns></returns
        [HttpPost("Controle")]
        public IActionResult Controle(IFormFile file)
        {
            if (file == null)
            {
                ViewBag.ErrorMessage = Resource.IMPORT_NOFILE;
                return View("Index");
            }
 
            //Contrôle token et company et création d'un repository
            var repository = APIRepository.Create(HttpContext.GetTokenAsync("access_token").Result, ApplicationSettings.CompanyId);
            if (repository.ErrorCode == "TOKENEXPIRED") return RedirectToRoute(Tools.forceAuthentication);
            if (repository.ErrorMessage != "") return View("Error", new Error(repository.ErrorMessage));

            //Chargement des paramètres (liste journaux, exercices, conditions réglement)
            Import existing = ReadParameters(repository);
            if (existing.ErrorMessage != null) return View("Error", new Error(existing.ErrorMessage));
            ApplicationSettings.ImportExistingValues = existing;
 
            // Ces deux méthodes vont ajouter des paires clé-valeurs aux dictionnaires pieces et errors.
            var pieces = new Dictionary<string, List<Writing>>();
            var errors = new Dictionary<string, List<string>>();

            // [API12] Import d'écritures - Traitement: contrôles avant import.
            ReadFile(repository, pieces, errors, existing, file);
            ApplicationSettings.ImportErrors = errors;
            ApplicationSettings.ImportPieces = pieces;

            ViewBag.errors = errors;
            ViewBag.pieces = pieces;
            ViewBag.nextStep =  4 ;
            ViewBag.filename = ApplicationSettings.ImportFileName=file.FileName;
            return View("Index");

        }

        //****************************[ETAPE IMPORTER]*******************************************************************
        /// <summary>
        /// Lit le fichier et ajoute les écritures correctes.
        /// </summary>
        /// <param name="file"> Le fichier importé. </param>
        /// <param name="historicalData"> Les données existantes permettant à convertir en objet Import. </param>
        /// <returns></returns
        [HttpPost("Import")]
        public IActionResult Import(string filename)
        {
            //Contrôle token et company et création d'un repository
            var repository = APIRepository.Create(HttpContext.GetTokenAsync("access_token").Result, ApplicationSettings.CompanyId);
            if (repository.ErrorCode == "TOKENEXPIRED") return RedirectToRoute(Tools.forceAuthentication);
            if (repository.ErrorMessage != "") return View("Error", new Error(repository.ErrorMessage));

            //Chargement des paramètres (liste journaux, exercices, conditions réglement) et contenu fichier depuis stockage effectué par l'étape Controle
            var existing = ApplicationSettings.ImportExistingValues;
            var errors = ApplicationSettings.ImportErrors;
            var pieces = ApplicationSettings.ImportPieces;

            // [API13] Import d'écritures - Traitement: import des écritures valides.
            AddPieces(repository, pieces, errors, existing);
            if (ApplicationSettings.ApiError!=null) return View("Error", ApplicationSettings.ApiError);

            ViewBag.errors = errors;
            ViewBag.pieces = pieces;
            ViewBag.nextStep = 5;
            ViewBag.filename = ApplicationSettings.ImportFileName;
            return View("Index");
        }

        //****************************[LECTURE FICHIER IMPORT ET CONTROLE COHERENCE]*******************************************************************
        /// <summary>
        /// Lit le fichier ligne par ligne.
        /// </summary>
        /// <param name="repository"> Le repository. </param>
        /// <param name="pieces"> Le dictionnaire de pièces contenant l'id de la pièce et la liste d'écritures. </param>
        /// <param name="errors"> Le dictionnaire de pièces contenant l'id de la pièce et la liste d'erreurs. </param>
        /// <param name="existing"> Les données préchargées (obligatoires) lors de l'arrivée sur la page d'import. </param>
        /// <param name="file"> Le fichier comptable texte importé. </param>
        private void ReadFile(APIRepository repository, Dictionary<string, List<Writing>> pieces, Dictionary<string, List<string>> errors, Import existing, IFormFile file)
        {
            using var reader = new StreamReader(file.OpenReadStream());
  
            var lineNumber = 0;
            var linePiece = 0;
            var line = string.Empty;

            while (!string.IsNullOrEmpty(line = reader.ReadLine()))
            {
                lineNumber++;

                //Si une ligne n'a pas le bon nombre de colonnes on sort une erreur grave et on continue le parcours pour voir si d'autres lignes dans ce cas
                if (Writing.HasIncorrectNumberOfColumns(line))
                {
                   AddErrorToPiece(lineNumber, "Nombre de colonnes incorrect", errors);
                   continue;
                }

                var writing = Writing.Create(line);

                // Contrôle cohérence et alimentation liste erreur.
                var errorLogs = new List<string>();
                writing.ControlWriting(repository, existing, errorLogs);

                // Ajout de l'écriture dans la pièce avec création préalable d'une nouvelle pièce si JOURNAL|PIECE|DATE n'existe pas dans la liste des pièces.
                if (!pieces.ContainsKey(writing.Id))
                {
                    linePiece = 0;
                    pieces.Add(writing.Id, new List<Writing>());
                }
                pieces[writing.Id].Add(writing);
                linePiece++;

                // Ajout des erreurs éventuelles de l'écriture avec création préalable d'une nouvelle liste d'erreurs pour la pièce si n'existe pas déjà
                if (errorLogs.Count > 0) AddErrorToPiece(writing.Id, linePiece, lineNumber, errorLogs, errors);

            }

            //Maintenant qu'on a dispatché les lignes en pièces, on peut vérifier si chaque pièce est équilibrée
            foreach (var piece in pieces)
            {
                if (!IsBalanced(piece))
                    AddErrorToPiece(piece.Key, "La pièce n'est pas équilibrée.", errors);
            }

 
            reader.Close();
        }
        private void AddErrorToPiece(string Key,string Message, Dictionary<string, List<string>> errors)
        {
            if (!errors.ContainsKey(Key)) errors.Add(Key, new List<string>());
            errors[Key].Add(Message);
        }
        private void AddErrorToPiece(int lineNumber, string Message, Dictionary<string, List<string>> errors)
        {
            var Key = "STRUCTURE";
            if (!errors.ContainsKey(Key)) errors.Add(Key, new List<string>());
            errors[Key].Add("Ligne "+lineNumber+" : "+Message);
        }

        private void AddErrorToPiece(string Key, int linePiece,int lineNumber, List<string> errorLogs, Dictionary<string, List<string>> errors)
        {
            if (!errors.ContainsKey(Key)) errors.Add(Key, new List<string>());
            errors[Key].AddRange(errorLogs.Select(s => "Ligne "+linePiece+" - Ligne fichier  "+lineNumber + " : "+s).ToList());            
        }

        /// <summary>
        /// Ajoute les écritures des pièces ne contenant pas d'erreurs.
        /// </summary>
        /// <param name="repository"> Le repository. </param>
        /// <param name="pieces"> Le dictionnaire des pièces, la clef étant l'id d'un objet Writing, la valeur étant la liste des écritures associées. </param>
        /// <param name="errors"> Le dictionnaire des erreurs, la clef étant l'id d'un objet Writing, la valeur étant la liste des erreurs associées. </param>
        /// <param name="existing"></param>
        private void AddPieces(APIRepository repository, Dictionary<string, List<Writing>> pieces, Dictionary<string, List<string>> errors, Import existing)
        {
            string data;
            foreach (var piece in pieces)
            {
                // Les écritures d'une pièce possédant des erreurs de validité ne seront pas ajoutées.
                if (errors.ContainsKey(piece.Key)) 
                    continue;
            
                if (1 == 1)
                {
                    //Purge des lignes de contrepartie si journal de trésorerie avec centralisation
                    //Calcul des ODataBind pour les propriétés concernées de chaque ligne 
                    var NewEcrituresToCreate = new List<Writing>();
                    foreach (var line in piece.Value)
                    {
                        if (existing.Codes[line.Code].OptContrepartie != "Centralise" || existing.Codes[line.Code].Compte != line.Compte)
                        {
                            SetOdataBindings(repository, line, existing);
                            NewEcrituresToCreate.Add(line);
                        }                          
                    }

                    //Création de l'entête proposition commerciale avec les éléments communs à la pièce
                    var CommonInfosPiece = piece.Value.First();
                    var NewPieceToCreate = CreerPieceComptable.Create(
                        CommonInfosPiece.Date,
                        NewEcrituresToCreate);
                    var CurrentJournalId=existing.Codes[CommonInfosPiece.Code].Id;

                    data = JsonConvert.SerializeObject(NewPieceToCreate).Replace("@odata.bind\":null", "\":null");
                    var result = repository.Post(repository.CompanyId, "journaux('"+CurrentJournalId+ "')/creerPieceComptable", data);
                    if (!result.IsSuccessStatusCode)
                    {
                        ViewBag.ErrorMessage = Tools.FormateErrorApi(result);
                        break;
                    }
                }

                //Ancienne méthode ligne à ligne, ne pas utiliser sauf raison précise car ne respecte pas toutes les règles métier dont le contrôle équilibre pièces
                /*
                foreach (var line in piece.Value)
                {
                    //Si journal avec option Centralisation et ligne du compte de centralisation on ignore car ajouté automatiquement par l'API.
            
                    if (existing.Codes[line.Code].OptContrepartie == "Centralise" && existing.Codes[line.Code].Compte == line.Compte)
                        continue;

                    SetOdataBindings(repository, line, existing);
                    data = JsonConvert.SerializeObject(line).Replace("@odata.bind\":null", "\":null");
                    var result = repository.Post(repository.CompanyId, "ecritures", data);
                    if (!result.IsSuccessStatusCode)
                    {
                        ViewBag.ErrorMessage = Tools.FormateErrorApi(result);
                        break;
                    }
                }
                */
                
            }
        }

        /// <summary>
        /// Retourne vrai si la pièce est équilibrée.
        /// </summary>
        /// <param name="piece"> La pièce dont il faut tester l'équilibre. </param>
        /// <returns> Vrai si la pièce est équilibrée. </returns>
        private bool IsBalanced(KeyValuePair<string, List<Writing>> piece)
        {
             return Tools.Round(piece.Value.Sum(p => p.Debit)) == Tools.Round(piece.Value.Sum(p => p.Credit));
        }

        /// <summary>
        /// Modifie les propriétés du modèle de sorte à ce que le OData Binding soit effectué.
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="line"> L'écriture comptable à exploiter. </param>
        /// <param name="existing"> L'objet Import contenant codes journaux, modes de réglement [...] connus. </param>
        private void SetOdataBindings(APIRepository repository, Writing line, Import existing)
        {
            // Bind code "journal" 
            var journalId = existing.Codes[line.Code].Id;
            line.CodeBind = Tools.OdataBind(repository, "journaux", journalId);

            // Bind "compte général"
            if (!string.IsNullOrEmpty(line.Compte))
            {
                var accountId = existing.Comptes[line.Compte];
                line.CompteBind = Tools.OdataBind(repository, "comptes", accountId);
            }
 
            // Bind id "tiers": Recherche du numero.
            if (!string.IsNullOrEmpty(line.NumeroTiers))
            {
                var tiersId = existing.Comptes[line.NumeroTiers];
                line.NumeroTiersBind = Tools.OdataBind(repository, "tiers", tiersId);
            }

            // Bind id de "modeReglement" si il existe.
            if (!string.IsNullOrEmpty(line.ModeReglement))
            {
                var modeReglementId = existing.IntitulesModesReglement[line.ModeReglement];
                line.ModeReglementBind = Tools.OdataBind(repository, "modesReglement", modeReglementId);
            }
         }

        /// <summary>
        /// Lecture des informations à ne pas relire à chaque contrôle d'une nouvelle ligne
        /// </summary>
        /// <param name="repository"></param>
        /// <returns>Objet Import contenant les informations</returns>
        private Import ReadParameters (APIRepository repository)
        {
            // Récupération des codes journaux, ces codes journaux seront accessibles en les recherchant directement par leurs indices (code).
            Dictionary<string, string> options = new Dictionary<string, string>();
            options.Add("$expand", "compte");
            options.Add("$select", "code,id,optContrepartie,compte");
            var result = repository.Get(repository.CompanyId, "journaux", options);
            if (!Tools.IsSuccess(result))
                return new Import { ErrorMessage = Tools.FormateErrorApi(result) };

            var journaux = result.GetJSONResult()["value"];
            Dictionary<string, JournalCache> codes = new Dictionary<string, JournalCache>();
            foreach (var journal in journaux)
            {
                codes.Add(journal["code"].ToString(), JournalCache.Create(journal) );
            }

            // Récupération des intitules de modes de réglements.
            options.Clear();
            options.Add("$select", "intitule,id");
            result = repository.Get(repository.CompanyId, "modesReglement", options);
            if (!Tools.IsSuccess(result))
                return new Import { ErrorMessage = Tools.FormateErrorApi(result) };

            var modesReglement = result.GetJSONResult()["value"];
            Dictionary<string, string> intitulesModesReglement = new Dictionary<string, string>();
            foreach (var mode in modesReglement)
            {
                intitulesModesReglement.Add(mode["intitule"].ToString(), mode["id"].ToString());
            }

            // Récupération des informations de la société afin d'obtenir les dates d'exercices.
            options.Clear();
            options.Add("$select", "exercices($select=dateDebut,dateFin;$filter=cloture eq false;$orderby=dateDebut desc;$top=1)");
            result = repository.Get(repository.CompanyId, "company", options);
            if (!Tools.IsSuccess(result))
                return new Import { ErrorMessage = Tools.FormateErrorApi(result) };

            var company = result.GetJSONResult();
            var exerciceStartDate = (DateTime)company["exercices"][0]["dateDebut"];
            var exerciceEndDate = (DateTime)company["exercices"][0]["dateFin"];

            //Structure pour tampon des comptes déjà lus, sera alimenté au fur et à mesure du parcours des lignes
            Dictionary<string, string> comptes = new Dictionary<string, string>();
            Dictionary<string, string> inconnuComptes = new Dictionary<string, string>();

            return new Import
            {
                Codes = codes,
                ExerciceStartDate = exerciceStartDate,
                ExerciceEndDate = exerciceEndDate,
                IntitulesModesReglement = intitulesModesReglement,
                Comptes=comptes,
                InconnuComptes=inconnuComptes,
                ErrorMessage = null
            };
       }

    }
}

