using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
namespace app.Models
{
    public class Telecom
    {
        [JsonProperty("telephone"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Telephone { get; set; }
        [JsonProperty("telecopie"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Telecopie { get; set; }
        [JsonProperty("site"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Site { get; set; }
        [JsonProperty("eMail"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public string EMail { get; set; }

        public Telecom()
        {
        }

        public Telecom(string site, string eMail, string telecopie, string telephone)
        {
            this.Site = site;
            this.EMail = eMail;
            this.Telecopie = telecopie;
            this.Telephone = telephone;
        }
    }
}
