using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace app.Models
{
    public class Customer
    {
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string commentaire { get; set; }
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string siret { get; set; }
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string identifiant { get; set; }
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string ape { get; set; }
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string contact { get; set; }
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string classement { get; set; }
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string qualite { get; set; }
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string intitule { get; set; }
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string type { get; set; }
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string numero { get; set; }
        [JsonProperty("comptePrincipal@odata.bind"), DisplayFormat(ConvertEmptyStringToNull = false)]
        public string comptePrincipal { get; set; }

        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string id { get; set; }

        public Adresse adresse { get; set; }
        public Telecom telecom { get; set; }

        public class Adresse
        {
            [DisplayFormat(ConvertEmptyStringToNull = false)]
            public string complement { get; set; }
            [JsonProperty("codePostal"), DisplayFormat(ConvertEmptyStringToNull = false)]
            public string codePostal { get; set; }
            [DisplayFormat(ConvertEmptyStringToNull = false)]
            public string adresse { get; set; }
            [DisplayFormat(ConvertEmptyStringToNull = false)]
            public string ville { get; set; }
            [DisplayFormat(ConvertEmptyStringToNull = false)]
            [JsonProperty("pays")]
            public string pays { get; set; }
            [JsonProperty("codeRegion"), DisplayFormat(ConvertEmptyStringToNull = false)]
            public string codeRegion { get; set; }
        }

        public class Telecom
        {
            [DisplayFormat(ConvertEmptyStringToNull = false)]
            public string telephone { get; set; }
            [DisplayFormat(ConvertEmptyStringToNull = false)]
            public string telecopie { get; set; }
            [DisplayFormat(ConvertEmptyStringToNull = false)]
            public string site { get; set; }
            [DisplayFormat(ConvertEmptyStringToNull = false)]
            public string eMail { get; set; }
        }
    }
}