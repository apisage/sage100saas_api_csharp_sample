using app.Repositories;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace app.Models
{
    public class Writing
    {
        /// <summary>
        /// Crée un objet Writing à partir d'une écriture de vente dans un fichier.
        /// </summary>
        /// <param name="line"> La ligne à convertir en objet. </param>
        /// <param name="lineNumber"> Le numéro de ligne que l'on souhaite lui affecter. </param>
        /// <returns></returns>
        public static Writing Create(string line, int lineNumber)
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
                LineNumber = lineNumber
            };

            writing.Echeance = (writing.Echeance == default(DateTime)) ? null : writing.Echeance;
            return writing;
        }

        [JsonIgnore]
        public string Id
        {
            get { return string.Concat(Code, '|', Piece, '|', Date.ToShortDateString()); }
        }
        [JsonIgnore]
        public int LineNumber { get; set; }
        [JsonProperty("journal@odata.bind"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Code { get; set; }
        [JsonProperty("date"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public DateTime Date { get; set; }
        [JsonProperty("piece"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Piece { get; set; }
        [JsonProperty("compte@odata.bind"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Compte { get; set; }
        [JsonProperty("tiers@odata.bind"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public string NumeroTiers { get; set; }
        [JsonProperty("intitule"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Intitule { get; set; }
        [JsonProperty("modeReglement@odata.bind"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public string ModeReglement { get; set; }
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

        /// <summary>
        /// Retourne une liste d'erreurs non vide si l'écriture comptable est invalide.
        /// </summary>
        /// <param name="repository"> Le repository permettant les appels API. </param>
        /// <param name="companyId"> La société concernée par les appels API. </param>
        /// <param name="existingCodes"> Les codes journaux existants. </param>
        /// <param name="existingPaymentTypes"> Les modes de réglements existants. </param>
        /// <param name="startDate"> La date de début de l'exercice non clôturé le plus récent. </param>
        /// <param name="endDate"> La date de fin de l'exercice non clôturé le plus récent. </param>
        /// <returns> Une liste de chaines comprenant les erreurs si détectés. </returns>
        public List<string> IsValidWriting(APIRepository repository, string companyId, Dictionary<string, string> existingCodes, Dictionary<string, string> existingPaymentTypes, DateTime startDate, DateTime endDate)
        {
            List<string> errors = new List<string>();
            var code = HasValidCode(existingCodes);
            var date = HasValidDate(startDate, endDate);
            var noPiece = HasValidNoPiece();
            var generalAccount = HasValidGeneralAccount(repository, companyId);
            var noTiers = HasValidNoTiers(repository, companyId);
            var intitule = HasValidIntitule();
            var modeReglement = HasValidPaymentType(existingPaymentTypes);
            var montant = HasValidDebitCredit();

            string[] properties = { code, date, noPiece, generalAccount, noTiers, intitule, modeReglement, montant };
            
            foreach (var property in properties)
            {
                var message = string.Concat("Pièce ", this.Piece, ", ligne ", this.LineNumber, ": ");

                if (!string.IsNullOrEmpty(property))
                {
                    message = string.Concat(message, property);
                    errors.Add(message);
                }
            }

            return errors;
        }

        /// <summary>
        /// Retourne un message d'erreur si le code de l'écriture courante est valide.
        /// </summary>
        /// <param name="existingCodes"> Un dictionnaire ayant en clef le code journal et en valeur l'id de ce de code journal. </param>
        /// <returns> Une chaîne indiquant l'erreur si détecté sinon une chaîne vide. </returns>
        public string HasValidCode(Dictionary<string, string> existingCodes)
        {
            if (!existingCodes.ContainsKey(this.Code))
            {
                var message = string.Concat("code journal '", this.Code, "' inexistant.");
                return message;
            }

            return string.Empty;
        }

        /// <summary>
        /// Retourne un message d'erreur si la date de l'écriture courante est valide.
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns> Une chaîne indiquant l'erreur si détecté sinon une chaîne vide. </returns>
        public string HasValidDate(DateTime startDate, DateTime endDate)
        {
            if (this.Date == null || !(startDate <= this.Date && this.Date <= endDate))
            {
                var message = string.Concat("La date '", this.Date, "' doit se situer entre le début et la fin du plus récent exercice ouvert au format DDMMYY.");
                return message;
            }

            return string.Empty;
        }
        /// <summary>
        /// Retourne un message d'erreur si le numéro de pièce de l'écriture courante est invalide.
        /// </summary>
        /// <returns> Une chaîne indiquant l'erreur si détectté sinon une chaîne vide. </returns>
        public string HasValidNoPiece()
        {
            var maxLength = 13;

            if (string.IsNullOrEmpty(this.Piece) || this.Piece.Length > maxLength)
            {
                return "numéro de pièce non conforme: vide ou d'une longueur de plus de 13 caractères.";
            }

            return string.Empty;
        }

        /// <summary>
        /// Retourne un message d'erreur si le compte général de l'écriture courante est invalide.
        /// </summary>
        /// <param name="repository"> Le repository permettant les appels API. </param>
        /// <param name="companyId"> La société concernée par les appels API. </param>
        /// <returns> Un message d'erreur si le compte général de l'écriture comptable courante est invalide. </returns>
        public string HasValidGeneralAccount(APIRepository repository, string companyId)
        {
            var filterParameter = "$filter";
            var topParameter = "$top";
            Dictionary<string, string> options = new Dictionary<string, string>();
            var accountFilter = "numero eq '{accountId}'";
            var filterAccountId = accountFilter.Replace("{accountId}", this.Compte);
            options[topParameter] = "1";
            options[filterParameter] = filterAccountId;
            var resultat = repository.Get(companyId, "comptes", options).GetJSONResult()["value"];

            if (!resultat.HasValues)
            {
                var message = string.Concat("le numéro de compte '", this.Compte, "' renseigné est inconnu.");
                return message;
            }

            return string.Empty;
        }

        /// <summary>
        /// Retourne un message d'erreur si le numéro de tiers de l'écriture courante est invalide.
        /// </summary>
        /// <param name="repository"> Le repository permettant les appels API. </param>
        /// <param name="companyId"> La société concernée par les appels API. </param>
        /// <returns> Une chaîne indiquant l'erreur si le numéro de tiers est invalide. </returns>
        public string HasValidNoTiers(APIRepository repository, string companyId)
        {
            Dictionary<string, string> options = new Dictionary<string, string>();
            var filterParameter = "$filter";
            var topParameter = "$top";
            var tiersFilter = "numero eq '{tiers}'";
            var filterNoTiers = tiersFilter.Replace("{tiers}", this.NumeroTiers);
            options[topParameter] = "1";
            options[filterParameter] = filterNoTiers;
            var resultat = repository.Get(companyId, "tiers", options).GetJSONResult()["value"];

            if (!string.IsNullOrEmpty(this.NumeroTiers) && (resultat == null || !resultat.HasValues))
            {
                var message = string.Concat("le numéro de tiers '", this.NumeroTiers, "' est inconnu.");
                return message;
            }

            return string.Empty;
        }

        /// <summary>
        /// Retourne un message d'erreur si le l'intitulé de l'écriture courante (si existant) est invalide.
        /// </summary>
        /// <returns> Une chaîne indiquant l'erreur si le numéro de tiers est invalide. </returns>
        public string HasValidIntitule()
        {
            if (!string.IsNullOrEmpty(this.Intitule) && this.Intitule.Length > 69)
            {
                return "Longueur de l'intitule de l'écriture est trop grande.";
            }

            return string.Empty;
        }

        /// <summary>
        /// Retourne un message d'erreur si le mode de réglement mentionné dans l'écriture courante est invalide.
        /// </summary>
        /// <param name="existingPaymentType"> Un dictionnaire ayant en clef l'intitulé du mode de réglement et en valeur l'id de l'intitulé. </param>
        /// <returns> Une chaîne indiquant l'erreur si le mode de réglement est invalide. </returns>
        public string HasValidPaymentType(Dictionary<string, string> existingPaymentType)
        {
            if (!string.IsNullOrEmpty(this.ModeReglement))
            {
                if (!existingPaymentType.ContainsKey(this.ModeReglement))
                {
                    var message = string.Concat("le mode de réglement '", this.ModeReglement, "'  est inconnu.");
                    return message;
                }
            }

            return string.Empty;
        }


        /// <summary>
        /// Retourne un message d'erreur lorque le sens de l'écriture est indéterminable selon les valeurs Débit et Crédit.
        /// </summary>
        /// <returns> Une chaîne indiquant l'erreur si le débit et le crédit ont des valeurs incohérentes par rapport au sens de l'écriture courante. </returns>
        public string HasValidDebitCredit()
        {
            var message = string.Empty;
            var debit = Tools.Round(this.Debit);
            var credit = Tools.Round(this.Credit);
            
            // Il est nécessaire que Debit ou Credit soit à 0.
            if (debit == credit)
            {
                message = string.Concat("le débit '", this.Debit, "' et le crédit '", this.Credit, "' indiqués sont incorrects.");

                return message;
            }

            // Le montant envoyé sera positif. Il faut, si besoin, changer le sens.
            var sens = debit != 0.0000 ? nameof(debit) : nameof(credit);

            if (debit + credit < 0.0000)
            {
                debit = Math.Abs(debit);
                credit = Math.Abs(credit);
                sens = sens.Equals("debit") ? "credit" : "debit";
            }
            
            this.Sens = string.Concat(sens.First().ToString().ToUpper(), sens.Substring(1));

            // Débit ou Crédit est forcémment égal à 0.
            this.Montant = debit + credit;

            return message;
        }
    }
}
