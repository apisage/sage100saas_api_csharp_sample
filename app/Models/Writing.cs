using app.Repositories;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace app.Models
{
    public class Writing
    {
        public static Writing Create(string line, int lineNumber)
        {
            string[] items = line.Split("\t");

            Writing writing = new Writing();
            writing.Code = items[1]; // code Journal
            writing.Date = Tools.ConvertFromTextToDateTime(items[2]);
            writing.Piece = items[3]; // noPiece
            writing.Compte = items[4]; // compte
            writing.NumeroTiers = items[5]; // noTiers
            writing.Intitule = items[6]; // libelle
            writing.ModeReglement = items[7]; // intitule du mode de réglement.
            writing.Echeance = Tools.ConvertFromTextToDateTime(items[8]);
            writing.Echeance = (writing.Echeance == default(DateTime)) ? null : writing.Echeance;
            writing.Debit = Double.Parse(items[9]);
            writing.Credit = Double.Parse(items[10]);
            writing.LineNumber = lineNumber;
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
        public string Code { get; set; } // ecritures('idEcriture')/journal
        [JsonProperty("date"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public DateTime Date { get; set; } // "date":    !!type DATETIME
        [JsonProperty("piece"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Piece { get; set; } // "piece": noPiece
        [JsonProperty("compte@odata.bind"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Compte { get; set; } // compteGeneral
        [JsonProperty("tiers@odata.bind"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public string NumeroTiers { get; set; } // compteTiers
        [JsonProperty("intitule"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Intitule { get; set; }
        [JsonProperty("modeReglement@odata.bind"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public string ModeReglement { get; set; }
        [JsonProperty("echeance"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public DateTime? Echeance { get; set; } // "echeance"
        [JsonProperty("sens"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Sens { get; set; }

        [JsonProperty("montant"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public double Montant { get; set; }
        [JsonIgnore]
        public double Debit { get; set; }
        [JsonIgnore]
        public double Credit { get; set; }

        public List<string> IsValidWriting(APIRepository repository, string companyId, Dictionary<string, string> existingCodes, Dictionary<string, string> existingPaymentTypes, DateTime startDate, DateTime endDate)
        {
            List<string> errors = new List<string>();
            var code = HasValidCode(existingCodes);
            var date = HasValidDate(startDate, endDate);
            var noPiece = HasValidNoPiece();
            var generalAccount = HasValidGeneralAccount(repository,companyId);
            var noTiers = HasValidNoTiers(repository,companyId);
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

        public string HasValidCode(Dictionary<string, string> existingCodes)
        {
            if (!existingCodes.ContainsKey(this.Code))
            {
                var message = string.Concat("code journal '", this.Code, "' inexistant.");
                return message;
            }
            return string.Empty;
        }

        public string HasValidDate(DateTime startDate, DateTime endDate)
        {
            // Test de validité de la date.
            if (this.Date == null || !(startDate <= this.Date && this.Date <= endDate))
            {
                var message = string.Concat("La date '", this.Date, "' doit se situer entre le début et la fin du plus récent exercice ouvert au format DDMMYY.");
                return message;
            }
            return string.Empty;
        }

        public string HasValidNoPiece()
        {
            var maxLength = 13;
            // Test de validité du numéro de pièce.
            if (string.IsNullOrEmpty(this.Piece) || this.Piece.Length > maxLength)
            {
                return "numéro de pièce non conforme: vide ou d'une longueur de plus de 13 caractères.";
            }
            return string.Empty;
        }

        public string HasValidGeneralAccount(APIRepository repository, string companyId)
        {
            var filterParameter = "$filter";
            var topParameter = "$top";
            // Test de validité du compte général.
            // Préparation du test d'existence du compte, $filter sera ajouté lors de la lecture du fichier.
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

        public string HasValidNoTiers(APIRepository repository, string companyId)
        {
            var filterParameter = "$filter";
            var topParameter = "$top";
            // Test de validité du numéro de tiers.
            // Préparation du test d'existence du numéro de tiers, $filter sera ajouté lors de la lecture du fichier.
            Dictionary<string, string> options = new Dictionary<string, string>();
            var tiersFilter = "numero eq '{tiers}'";
            var filterNoTiers = tiersFilter.Replace("{tiers}", this.NumeroTiers);
            options[topParameter] = "1";
            options[filterParameter] = filterNoTiers;
            var resultat = repository.Get(companyId, "tiers", options).GetJSONResult()["value"];
            if (!string.IsNullOrEmpty(this.NumeroTiers) && !resultat.HasValues)
            {
              var message = string.Concat("le numéro de tiers '", this.NumeroTiers, "' est inconnu.");
              return message;
            }
            return string.Empty;
        }

        public string HasValidIntitule()
        {
            // Test de validité de l'intitulé de l'écriture.
            if (!string.IsNullOrEmpty(this.Intitule) && this.Intitule.Length > 69)
            {
                return "Longueur de l'intitule de l'écriture est trop grande.";
            }
            return string.Empty;
        }

        public string HasValidPaymentType(Dictionary<string, string> existingPaymentType)
        {

            // Test de validité du mode de réglement.
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

        public string HasValidDebitCredit()
        {
            var message = string.Empty;
            var roundDebit = Tools.Round(this.Debit);
            var roundCredit = Tools.Round(this.Credit);

            if(roundDebit > roundCredit && roundCredit == 0.0000)
            {
                this.Montant = roundDebit;
                this.Sens = "Debit";
            }
            else if(roundCredit > roundDebit && roundDebit == 0.0000)
            {
                this.Montant = roundCredit;
                this.Sens = "Credit";
            }
            else
            {
                message = string.Concat("le débit '", this.Debit, "' et le crédit '", this.Credit  ,"' indiqués sont incorrects.");
            }
            return message;
        }
    }
}
