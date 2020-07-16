using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Net.Http;

namespace app.Repositories
{
    public static class Tools
    {
        /// <summary>
        /// Détermine si la réponse à une requête est valide ou non.
        /// </summary>
        /// <param name="message"> La réponse à une requête. </param>
        /// <returns></returns>
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

        /// <summary>
        /// Vérifie si la réponse d'une requête correspond à un format JSON.
        /// </summary>
        /// <param name="response"> La reponse d'une requête devant être vérifiée. </param>
        /// <returns></returns>
        public static bool IsJson(this string response)
        {
            try
            {
                var token = JToken.Parse(response);
                return token.Type == JTokenType.Object || token.Type == JTokenType.Array;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Transforme à partir d'une réponse à une requête, des données JSON en JObject.
        /// </summary>
        /// <param name="message"> La réponse à une requête. </param>
        /// <returns> Un JObject avec les données correspondantes. </returns>
        public static JObject GetJSONResult(this HttpResponseMessage message)
        {
            var response = message.Content.ReadAsStringAsync().Result;
            var result = new JObject();

            if (!string.IsNullOrEmpty(response) && response.IsJson())
            {
                result = JObject.Parse(response);
            }

            return result;
        }

        /// <summary>
        /// Transforme à partir d'une réponse à une requête, des données JSON pour les afficher au format string.
        /// </summary>
        /// <param name="message"> La réponse à une requête. </param>
        /// <returns> Retourne une chaine de caractère représentant un JSON. </returns>
        public static string GetStringResult(this HttpResponseMessage message)
        {
            var data = GetJSONResult(message);
            string responseContent = data.ToString();
            dynamic parsedJson = JsonConvert.DeserializeObject(responseContent);

            return JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
        }

        /// <summary>
        /// Arrondit un double à 4 décimales.
        /// </summary>
        /// <param name="value"> La valeur à arrondir. </param>
        /// <returns> La valeur arrondie à 4 décimales. </returns>
        public static double Round(double value)
        {
            return System.Math.Round(value, 4, System.MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Convertit une chaine représentant une date format DDMMYY au format DateTime.
        /// </summary>
        /// <param name="date"> La date DDMMYY à convertir. </param>
        /// <returns> Retourne l'objet DateTime correspondant à la date si valide. </returns>
        public static DateTime ConvertFromTextToDateTime(string date)
        {
            if (string.IsNullOrEmpty(date) || !date.Length.Equals(6))
            {
                return default;
            }

            var day = Int32.Parse(date.Substring(0, 2));
            var month = Int32.Parse(date.Substring(2, 2));
            var year = Int32.Parse(date.Substring(4, 2)) + 2000;

            return new DateTime((int)year, (int)month, (int)day, 0, 0, 0, DateTimeKind.Utc);
        }
    }
}
