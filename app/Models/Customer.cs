using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace app.Models
{
    public class Customer
    {
        [JsonProperty("commentaire"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Commentaire { get; set; }
        [JsonProperty("siret"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Siret { get; set; }
        [JsonProperty("identifiant"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Identifiant { get; set; }
        [JsonProperty("ape"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Ape { get; set; }
        [JsonProperty("contact"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Contact { get; set; }
        [JsonProperty("classement"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Classement { get; set; }
        [JsonProperty("qualite"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Qualite { get; set; }
        [JsonProperty("intitule"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Intitule { get; set; }
        [JsonProperty("type"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Type { get; set; }
        [JsonProperty("numero"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Numero { get; set; }
        [JsonProperty("comptePrincipal@odata.bind"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public string ComptePrincipal { get; set; }

        [JsonProperty("id"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Id { get; set; }
        public Address Adresse { get; set; }
        public Telecom Telecom { get; set; }
        
        public static bool IsEmpty(Customer customer)
        {
            string empty = string.Empty;
            bool numero = empty.Equals(customer.Numero);
            bool type = empty.Equals(customer.Type);
            bool intitule = empty.Equals(customer.Intitule);

            return numero || type || intitule;
        }

        public static bool IsNull(Customer customer)
        {
            bool numero = (null == customer.Numero);
            bool type = (null == customer.Type);
            bool intitule = (null == customer.Intitule);

            return (numero || type || intitule);
        }
        public Customer()
        {
        }
    }
}