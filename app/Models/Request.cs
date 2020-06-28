using System.ComponentModel.DataAnnotations;

namespace app.Models
{
    public class Request
    {
        public string RespStatusCode { get; set; }
        public string RespBody { get; set; }
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Company { get; set; }
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Expand { get; set; }
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Filter { get; set; }
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Select { get; set; }
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Orderby { get; set; }
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Top { get; set; }
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Skip { get; set; }
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Count { get; set; }
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Resource { get; set; }
    }
}