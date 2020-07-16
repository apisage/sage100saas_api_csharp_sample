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
    }
}
