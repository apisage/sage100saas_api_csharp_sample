using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using app.Settings;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using app.Models;

namespace app
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            InitializeApplicationSettings();
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

  

        //Fix impossible de se connecter en http : https://github.com/dotnet/aspnetcore/issues/14996
        private void CheckSameSite(HttpContext httpContext, CookieOptions options)
        {
            if (options.SameSite == SameSiteMode.None && !httpContext.Request.IsHttps)
            {
                options.SameSite = SameSiteMode.Unspecified;
            }
        }

        // [API14] Authentification - Configuration.
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLocalization();
            services.AddRazorPages().AddRazorRuntimeCompilation();
            services.AddDistributedMemoryCache();
            services.AddSession(options =>
            {
                options.Cookie.HttpOnly = false;
                options.Cookie.IsEssential = true;
                options.IdleTimeout = TimeSpan.FromHours(1);
            });

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
                //Fix impossible de se connecter en http : https://github.com/dotnet/aspnetcore/issues/14996
                options.OnAppendCookie = cookieContext => CheckSameSite(cookieContext.Context, cookieContext.CookieOptions);
                options.OnDeleteCookie = cookieContext => CheckSameSite(cookieContext.Context, cookieContext.CookieOptions);
            });

            // Add authentication services
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie()
            .AddOpenIdConnect("SageId", options =>
            {
                // Set the authority to your SageId domain
                options.Authority = $"https://{ApplicationSettings.Authority}";

                // Configure the SageId Client ID and Client Secret
                options.ClientId = ApplicationSettings.ClientId;
                options.ClientSecret = ApplicationSettings.ClientSecret;

                // Set response type to code
                options.ResponseType = OpenIdConnectResponseType.Code;

                // Configure the scope
                options.Scope.Add("openid");
                options.Scope.Add("offline_access"); //=> Pour obtenir le refresh_token
                options.Scope.Add("Company.Read.All");
                options.Scope.Add("Parameter.Read.All");
                options.Scope.Add("Parameter.Write.All");
                options.Scope.Add("Account.Read.All");
                options.Scope.Add("Tiers.Write.All");
                options.Scope.Add("Tiers.Read.All");
                options.Scope.Add("Tax.Read.All");
                options.Scope.Add("Journal.Read.All");
                options.Scope.Add("History.Accounting.Read.All");
                options.Scope.Add("History.Accounting.Write.All");


                // Set the callback path
                // Also ensure that you have added the URL as an Allowed Callback URL in your SageId dashboard
                options.CallbackPath = ApplicationSettings.CallBackPath;
               
                // Configure the Claims Issuer to be SageId
                options.ClaimsIssuer = "SageId";
                options.SaveTokens = true;
                options.Events = new OpenIdConnectEvents
                {
                    // handle the logout redirection
                    OnRedirectToIdentityProviderForSignOut = (context) =>
                    {
                        var logoutUri = $"https://{ApplicationSettings.Authority}/v2/logout?client_id={options.ClientId}";
                        var postLogoutUri = context.Properties.RedirectUri;
                        if (!string.IsNullOrEmpty(postLogoutUri))
                        {
                            if (postLogoutUri.StartsWith("/"))
                            {
                                // transform to absolute
                                var request = context.Request;
                                postLogoutUri = request.Scheme + "://" + request.Host + request.PathBase + postLogoutUri;
                            }
                            logoutUri += $"&returnTo={ Uri.EscapeDataString(postLogoutUri)}";
                        }

                        context.Response.Redirect(logoutUri);
                        context.HandleResponse();

                        return Task.CompletedTask;
                    },
                    OnRedirectToIdentityProvider = context =>
                    {
                        context.ProtocolMessage.SetParameter("audience", ApplicationSettings.Audience);

                        return Task.FromResult(0);
                    }
                };
            });
            services.AddMvc();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }
            app.UseStaticFiles();
            app.UseSession();
            app.UseCookiePolicy();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}"
                );
                endpoints.MapControllerRoute(
                    name: "request",
                    pattern: "{controller=Request}/{action=Index}"
                );
                endpoints.MapControllerRoute(
                    name: "customer",
                    pattern: "{controller=Customers}/{action=Index}"
                );
                endpoints.MapControllerRoute(
                    name: "customerAdd",
                    pattern: "{controller=Customers}/{action=Add}"
                );
                endpoints.MapControllerRoute(
                    name: "import",
                    pattern: "{controller=Import}/{action=Index}"
                );
                endpoints.MapControllerRoute(
                    name: "error",
                    pattern: "{controller=Home}/{action=Error}"
                );
                endpoints.MapControllerRoute(
                    name: "authentificationLogin",
                    pattern: "{controller=Authentification}/{action=Login}"
                );
                endpoints.MapControllerRoute(
                    name: "authentificationLogout",
                    pattern: "{controller=Authentification}/{action=Logout}"
                );
            });
        }

        //Recherche la présence du fichier client_application.json 
        public static string GetPathOfConfigFile()
        {
            if (System.IO.File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "client_application.json")))
            {
                return Path.Combine(Directory.GetCurrentDirectory(), "client_application.json");
            }

            return "";
        }

        public static void InitializeApplicationSettings()
        {
            ApplicationSettings.ClientId = "default";

            var settingsPath = GetPathOfConfigFile();
            if (!string.IsNullOrEmpty(settingsPath))
            {
                StreamReader file = File.OpenText(settingsPath);
                using JsonTextReader reader = new JsonTextReader(file);
                JObject configObj = (JObject)JToken.ReadFrom(reader);
                ApplicationSettings.ClientId = (string)configObj["config"]["client_id"];
                ApplicationSettings.ClientSecret = (string)configObj["config"]["client_secret"];
                ApplicationSettings.CompanyName = (string)configObj["config"]["company_name"];

                var alternateUrlApi = (string)configObj["config"]["url_api"];
                ApplicationSettings.UrlApi = (string.IsNullOrEmpty(alternateUrlApi)) ? ApplicationSettings.DefaultUrlApi : alternateUrlApi;
                if (!ApplicationSettings.UrlApi.EndsWith("/")) ApplicationSettings.UrlApi += "/";

                var alternateUrlManagement = (string)configObj["config"]["url_management"];
                ApplicationSettings.UrlManagement = (string.IsNullOrEmpty(alternateUrlManagement)) ? ApplicationSettings.DefaultUrlManagement : alternateUrlManagement;
            }
        }
    }
}
