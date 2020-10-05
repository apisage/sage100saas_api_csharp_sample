using System;
using System.Collections.Generic;
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

        public Customer(Address address, Telecom telecom)
        {
            Adresse = address;
            Telecom = telecom;
        }
    }
	
	public class ColumnsListCustomer
    {
        public static ColumnsListCustomer Create(string Name, string Property = "", string Size = "15%", bool Sorting = false)
        {
            return new ColumnsListCustomer
            {
                Title = Name,
                Property = Property,
                Size = Size,
                Sorting = Sorting
           };
        }

        public static Dictionary<string, ColumnsListCustomer> LoadColumns()
        {
            Dictionary<string, ColumnsListCustomer> columns = new Dictionary<string, ColumnsListCustomer>
            {
                { "1", ColumnsListCustomer.Create("Numéro", "numero", "15%",true) },
                { "2", ColumnsListCustomer.Create("Intitulé", "intitule","20%", true) },
                { "3", ColumnsListCustomer.Create("Adresse","adresse/pays,adresse/ville", "40%",true)},
                { "4", ColumnsListCustomer.Create("Téléphone",null,"14%") },
                { "5", ColumnsListCustomer.Create("Solde",null,"10%") }
            };
            return columns;
        }

        public string Title { get; set; }
        public string Property { get; set; }
        public string Size { get; set; }
        public bool Sorting { get; set; }
    }

}