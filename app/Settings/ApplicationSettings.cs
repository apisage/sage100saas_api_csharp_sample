
using app.Models;
using app.Repositories;
using System.Collections.Generic;
using System.Net.Http;

namespace app.Settings
{
    public static class ApplicationSettings
    {
        public const string DefaultUrlApi = "https://api-stg.100saasbeta.fr/";
        public const string DefaultUrlManagement = "https://stg-bureau.sage.fr"; 

        public static string Authority { get => "id-shadow.sage.com"; }
        public static string Audience { get => "fr100saas/api.pub"; }
        public static string CallBackPath { get => "/auth/callback"; }
        public static string ClientId { get; set; }
        public static string ClientSecret { get; set; }
        public static string CompanyName { get; set; }
        public static string CompanyId { get; set; }
        public static HttpResponseMessage CompaniesCache { get; set; }
        public static string UrlApi { get; set; }
        public static string UrlManagement { get; set; }

        public static System.Xml.XmlDocument MetadataCache { get; set; }
        public static Dictionary<string, Tools.MainResources> MetadataCacheResources { get; set; }
        public static string MessageInfo { get; set; }
        public static Error ApiError { get; set; }
        public static List<APIRepository.HistoApis> LastAPIs { get; set; }

        //Sauvegardes temporaires entre contrôle et import pour module Import
        public static Import ImportExistingValues { get; set; }
        public static Dictionary<string, List<Writing>> ImportPieces { get; set; }
        public static Dictionary<string, List<string>> ImportErrors { get; set; }
        public static string ImportFileName { get; set; }

    }
}
