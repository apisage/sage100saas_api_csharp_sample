using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace app.Models
{
    //Ensemble des informations mises en cache au début (journaux,règlements) ou en cours de parcours des lignes (comptes et tiers).
    public class Import
    {
        public Dictionary<string, JournalCache> Codes { get; set; }
        public DateTime ExerciceStartDate { get; set; }
        public DateTime ExerciceEndDate { get; set; }
        public Dictionary<string, string> IntitulesModesReglement { get; set; }
        public Dictionary<string, string> Comptes { get; set; }
        public Dictionary<string,string> InconnuComptes { get; set; }
        public string ErrorMessage{get;set;}
    }

    // Structure d'un journal mis en cache, on a besoin en plus de l'Id de l'option contrepartie+compte pour le cas des journaux de trésorerie.
    public class JournalCache
    {
        public static JournalCache Create(JToken journal)
        {
            return new JournalCache
            {
                Code = journal["code"].ToString(),
                Id = journal["id"].ToString(),
                OptContrepartie = journal["optContrepartie"].ToString(),
                Compte = (journal["compte"].ToString() != "") ? journal["compte"]?["numero"].ToString() : ""
            };
        }
        public string Code { get; set; }
        public string Id { get; set; }
        public string OptContrepartie { get; set; }
        public string Compte { get; set; }
    }
}
