using app.Repositories;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace app.Models
{
    public class CreerPieceComptable
    {

        public static CreerPieceComptable Create(DateTime Date,String Piece, String Reference, String NumeroFacture, String Intitule,List<Writing> Ecritures )
        {
            CreerPieceComptable creerPieceComptable = new CreerPieceComptable
            {
                Date = Date,
                Reference = Reference,
                NumeroFacture = NumeroFacture,
                Piece = Piece,
                Intitule = Intitule,
                Ecritures = Ecritures
            };
            return creerPieceComptable;
        }

        [JsonProperty("date"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public DateTime Date { get; set; }

        [JsonProperty("piece"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Piece { get; set; }

        [JsonProperty("reference"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Reference { get; set; }

        [JsonProperty("numeroFacture"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public string NumeroFacture { get; set; }

        [JsonProperty("intitule"), DisplayFormat(ConvertEmptyStringToNull = true)]
        public string Intitule { get; set; }

        [JsonProperty("ecritures"), DisplayFormat(ConvertEmptyStringToNull = true)]
        public List<Writing> Ecritures { get; set; }
    }

    public class Writing
    {
        /// <summary>
        /// Crée un objet Writing à partir d'une écriture de vente dans un fichier.
        /// </summary>
        /// <param name="line"> La ligne à convertir en objet. </param>
        /// <returns></returns>
        public static Writing Create(string line)
        {
            string[] items = line.Split("\t");

            Writing writing = new Writing
            {
                Code = items[1],
                Date = Tools.ConvertFromTextToDateTime(items[2]),
                Piece = items[3],
                Compte = items[4],
                NumeroTiers = items[5],
                Intitule = items[6],
                ModeReglement = items[7],
                Echeance = Tools.ConvertFromTextToDateTime(items[8]),
                Debit = Double.Parse(items[9]),
                Credit = Double.Parse(items[10]),
                Reference="",
                NumeroFacture="",
            };

            writing.Echeance = (writing.Echeance == default(DateTime)) ? null : writing.Echeance;

            return writing;
        }

        [JsonIgnore]
        public string Id{get { return string.Concat(Code, '|', Piece, '|', Date.ToShortDateString()); }}
 
        [JsonIgnore]
        public string Code { get; set; }
        [JsonProperty("journal@odata.bind"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public string CodeBind { get; set; }

        [JsonProperty("date"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public DateTime Date { get; set; }
        
        [JsonProperty("piece"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Piece { get; set; }

        [JsonProperty("reference"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Reference { get; set; }

        [JsonProperty("numeroFacture"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public string NumeroFacture { get; set; }

        [JsonIgnore]
        public string Compte { get; set; }
        [JsonProperty("compte@odata.bind"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public string CompteBind { get; set; }

        [JsonIgnore]
        public string NumeroTiers { get; set; }
        [JsonProperty("tiers@odata.bind"), DisplayFormat(ConvertEmptyStringToNull = true)]
        public string NumeroTiersBind { get; set; }
        
        [JsonProperty("intitule"), DisplayFormat(ConvertEmptyStringToNull = true)]
        public string Intitule { get; set; }

        [JsonIgnore]
        public string ModeReglement { get; set; }
        [JsonProperty("modeReglement@odata.bind"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public string ModeReglementBind { get; set; }
        
        [JsonProperty("echeance"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public DateTime? Echeance { get; set; }
        
        [JsonProperty("sens"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Sens { get; set; }

        [JsonProperty("montant"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public double Montant { get; set; }
        [JsonIgnore]
        public double Debit { get; set; }
        [JsonIgnore]
        public double Credit { get; set; }

        //Contrôle nombre de colonnes correct d'une ligne
        public static bool HasIncorrectNumberOfColumns(string line)
        {
            return (line.Split("\t").Length != 11);
        }


        /// <summary>
        /// Retourne une liste d'erreurs non vide si l'écriture comptable est invalide.
        /// </summary>
        /// <param name="repository"> Le repository permettant les appels API. </param>
        /// <param name="existing"> L'objet Import composé des codes journaux, modes de réglement existants et les dates de l'exercice non clôturé le plus récent. </param>
        /// <returns> Une liste de chaines comprenant les erreurs si détectés. </returns>
        public void ControlWriting(APIRepository repository, Import existing, List<string> errorLogs)
        {
            ControlCodeJournal(existing.Codes, errorLogs);
            ControlDate(existing.ExerciceStartDate, existing.ExerciceEndDate, errorLogs);
            ControlNoPiece(errorLogs);
            ControlGeneralAccount(repository, existing.Comptes, existing.InconnuComptes,errorLogs);
            ControlNoTiers(repository, existing.Comptes, existing.InconnuComptes,errorLogs);
            ControlIntitule(errorLogs);
            ControlPaymentType(existing.IntitulesModesReglement, errorLogs);
            ControlMontant(errorLogs);
        }

        /// <summary>
        /// Retourne un message d'erreur si le code de l'écriture courante est valide.
        /// </summary>
        /// <param name="existingCodes"> Un dictionnaire ayant en clef le code journal et en valeur l'id de ce de code journal. </param>
        /// <returns> Une chaîne indiquant l'erreur si détecté sinon une chaîne vide. </returns>
        public void ControlCodeJournal(Dictionary<string, JournalCache> existingCodes, List<string> errors)
        {
            if (!existingCodes.ContainsKey(this.Code))
                errors.Add("Le code journal '"+ this.Code+"' est inconnu.");
        }

        /// <summary>
        /// Retourne un message d'erreur si la date de l'écriture courante est valide.
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns> Une chaîne indiquant l'erreur si détecté sinon une chaîne vide. </returns>
        public void ControlDate(DateTime startDate, DateTime endDate, List<string> errors)
        {
            if (this.Date == null || !(startDate <= this.Date && this.Date <= endDate))
                 errors.Add("La date '"+ this.Date.ToShortDateString() + "' n'est pas dans l'exercice courant.");
        }
        /// <summary>
        /// Retourne un message d'erreur si le numéro de pièce de l'écriture courante est invalide.
        /// </summary>
        /// <returns> Une chaîne indiquant l'erreur si détectté sinon une chaîne vide. </returns>
        public void ControlNoPiece(List<string> errors)
        {
            if (string.IsNullOrEmpty(this.Piece) || this.Piece.Length > 13)
                errors.Add("Le numéro de pièce est non conforme: vide ou d'une longueur de plus de 13 caractères.");
        }

        /// <summary>
        /// Retourne un message d'erreur si le compte général de l'écriture courante est invalide.
        /// </summary>
        /// <param name="repositor
        /// y"> Le repository permettant les appels API. </param>
        /// <param name="companyId"> La société concernée par les appels API. </param>
        /// <returns> Un message d'erreur si le compte général de l'écriture comptable courante est invalide. </returns>
        public void ControlGeneralAccount(APIRepository repository, Dictionary<string, string> existingCompte, Dictionary<string, string> existingInconnuCompte, List<string> errors)
        {
            if (existingCompte.ContainsKey(this.Compte)) return;
            if (existingInconnuCompte.ContainsKey(this.Compte))
            {
                errors.Add("Le numéro de compte '" + this.Compte + "' est inconnu.");
                return;
            }

            Dictionary<string, string> options = new Dictionary<string, string>();
            options.Add("$top","1");
            options.Add("$filter","numero eq '" + this.Compte + "'");
            var resultat = repository.Get(repository.CompanyId, "comptes", options).GetJSONResult()["value"];

            if (!resultat.HasValues)
            {
                errors.Add("Le numéro de compte '" + this.Compte + "' est inconnu.");
                existingInconnuCompte.Add(this.Compte, "");
            }
            else
                existingCompte.Add(this.Compte, resultat[0]["id"].ToString());
            
        }

        /// <summary>
        /// Retourne un message d'erreur si le numéro de tiers de l'écriture courante est invalide.
        /// </summary>
        /// <param name="repository"> Le repository permettant les appels API. </param>
        /// <param name="companyId"> La société concernée par les appels API. </param>
        /// <returns> Une chaîne indiquant l'erreur si le numéro de tiers est invalide. </returns>
        public void ControlNoTiers(APIRepository repository, Dictionary<string, string> existingCompte, Dictionary<string, string> existingInconnuCompte, List<string> errors)
        {
            if (string.IsNullOrEmpty(this.NumeroTiers) || existingCompte.ContainsKey(this.NumeroTiers)) return;
            if (existingInconnuCompte.ContainsKey(this.NumeroTiers))
            {
                errors.Add("Le numéro de tiers '" + this.NumeroTiers + "' est inconnu.");
                return;
            }

            Dictionary<string, string> options = new Dictionary<string, string>();
            options.Add("$top", "1");
            options.Add("$filter", "numero eq '" + this.NumeroTiers + "'");
            var resultat = repository.Get(repository.CompanyId, "tiers", options).GetJSONResult()["value"];

            if (!resultat.HasValues)
            {
                errors.Add("Le numéro de tiers '" + this.NumeroTiers + "' est inconnu.");
                existingInconnuCompte.Add(this.NumeroTiers, "");
            }
            else
                existingCompte.Add(this.NumeroTiers, resultat[0]["id"].ToString());

        }

        /// <summary>
        /// Retourne un message d'erreur si le l'intitulé de l'écriture courante (si existant) est invalide.
        /// </summary>
        /// <returns> Une chaîne indiquant l'erreur si le numéro de tiers est invalide. </returns>
        public void ControlIntitule(List<string> errors)
        {
            if (!string.IsNullOrEmpty(this.Intitule) && this.Intitule.Length > 69)
                errors.Add("La longueur de l'intitule de l'écriture dépasse 69 caractères");
        }

        /// <summary>
        /// Retourne un message d'erreur si le mode de réglement mentionné dans l'écriture courante est invalide.
        /// </summary>
        /// <param name="existingPaymentType"> Un dictionnaire ayant en clef l'intitulé du mode de réglement et en valeur l'id de l'intitulé. </param>
        /// <returns> Une chaîne indiquant l'erreur si le mode de réglement est invalide. </returns>
        public void ControlPaymentType(Dictionary<string, string> existingPaymentType, List<string> errors)
        {
            if (string.IsNullOrEmpty(this.ModeReglement)) return;
    
            if (!existingPaymentType.ContainsKey(this.ModeReglement))
                errors.Add("Le mode de réglement '"+ this.ModeReglement+ "'  est inconnu.");
        }


        /// <summary>
        /// Retourne un message d'erreur lorque le sens de l'écriture est indéterminable selon les valeurs Débit et Crédit.
        /// </summary>
        /// <returns> Une chaîne indiquant l'erreur si le débit et le crédit ont des valeurs incohérentes par rapport au sens de l'écriture courante. </returns>
        public void ControlMontant(List<string> errors)
        {
            var debit = Tools.Round(this.Debit);
            var credit = Tools.Round(this.Credit);

            // Il est nécessaire que Debit ou Credit soit à 0.
            if (debit == credit)
                errors.Add("L'une des colonnes Débit '" + this.Debit + "' ou Crédit '" + this.Credit + "' doit être à 0");
            else
            {
                // Détermination de la propriété sens qui doit contenir "Debit" ou "Credit" et de montant qui contient la valeur 
                this.Sens = debit != 0d ? "Debit" : "Credit";
                this.Montant = debit + credit;
                if (this.Montant < 0d) this.Sens = (this.Sens == "Debit") ? "Credit" : "Debit";
            }            
        }
    }
}
