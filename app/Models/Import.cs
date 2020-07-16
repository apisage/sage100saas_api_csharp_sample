using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace app.Models
{
    public class Import
    {
        public IFormFile File { get; set; }
        public Dictionary<string,string> Codes { get; set; }
        public DateTime ExerciceStartDate { get; set; }
        public DateTime ExerciceEndDate { get; set; }
        public Dictionary<string, string> IntitulesModesReglement { get; set; }
    }
}
