using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace app.Models
{
    public class ChangeCompany
    {
        [JsonProperty("companyid"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public string CompanyId { get; set; }
        [JsonProperty("companyname"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public string CompanyName { get; set; }
        [JsonProperty("clearcache"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Clearcache { get; set; }
    }
}