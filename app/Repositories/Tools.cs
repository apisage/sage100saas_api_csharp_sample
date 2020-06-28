using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Net.Http;

namespace app.Repositories
{
    public static class Tools
    {
        public static bool IsSuccess(this HttpResponseMessage message)
        {
            if (message.IsSuccessStatusCode)
            {
                return true;
            }
            switch (message.StatusCode)
            {
                case HttpStatusCode.Created: // 201.
                case HttpStatusCode.Accepted: // 202.
                case HttpStatusCode.NonAuthoritativeInformation: // 203.
                case HttpStatusCode.NoContent: // 204.
                case HttpStatusCode.ResetContent: // 205.
                case HttpStatusCode.PartialContent: // 206.
                    return true;
                default:
                    return false;
            }
        }

        public static JObject GetJSONResult(this HttpResponseMessage message)
        {
            var response = message.Content.ReadAsStringAsync().Result;
            return string.IsNullOrEmpty(response) ? new JObject() : JObject.Parse(response);
        }

        public static string GetStringResult(this HttpResponseMessage message)
        {
            // Récupération du résultat.
            var data = GetJSONResult(message);
            string responseContent = data.ToString();
            dynamic parsedJson = JsonConvert.DeserializeObject(responseContent);
            return JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
        }

        public static double Round(double value)
        {
            return System.Math.Round(value, 4, System.MidpointRounding.AwayFromZero);
        }

        public static DateTime ConvertFromTextToDateTime(string date)
        {
            if(string.IsNullOrEmpty(date) || !date.Length.Equals(6))
            {
                return default(DateTime);
            }
            var day = Int32.Parse(date.Substring(0, 2));
            var month = Int32.Parse(date.Substring(2, 2));
            var year = Int32.Parse(date.Substring(4, 2)) + 2000;
            return new DateTime((int)year, (int)month, (int)day, 0, 0, 0, DateTimeKind.Utc);
        }
    }
}
