using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace app.Models
{
    public class Address
    {
        [JsonProperty("complement"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Complement { get; set; }
        [JsonProperty("codePostal"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public string CodePostal { get; set; }
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string adresse { get; set; }
        [JsonProperty("ville"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Ville { get; set; }
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        [JsonProperty("pays")]
        public string Pays { get; set; }
        [JsonProperty("codeRegion"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public string CodeRegion { get; set; }

        public Address()
        {
        }

        public Address(string adresse, string pays, string complement, string codePostal, string codeRegion, string ville)
        {
            this.adresse = adresse;
            this.Pays = pays;
            this.Complement = complement;
            this.CodePostal = codePostal;
            this.CodeRegion = codeRegion;
            this.Ville = ville;
        }
    }
}
