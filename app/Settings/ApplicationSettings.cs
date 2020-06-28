using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace app.Settings
{
    public static class ApplicationSettings
    {
        public static string client_id { get; set; }
        public static string client_secret { get; set; }
        public static string callback_url { get; set; }
        public static string company_name { get; set; }
    }
}
