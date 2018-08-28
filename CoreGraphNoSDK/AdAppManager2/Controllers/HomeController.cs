using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using AdAppManager2.Models;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;

namespace AdAppManager2.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IConfiguration _configuration;


        public HomeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            string thisAppID = _configuration["ClientId"];
            string thisAppRedirectURL = $"{_configuration["AppUrl"]}";
            string thisUserTenantID = User.Claims.Where(x => x.Type == "http://schemas.microsoft.com/identity/claims/tenantid").FirstOrDefault().Value;
            string adminconsenturl = $"https://login.microsoftonline.com/{thisUserTenantID}/adminconsent?client_id={thisAppID}&state=12345&redirect_uri={thisAppRedirectURL}";
            ViewBag.adminconesenturl = adminconsenturl;
            return View();
        }

        public IActionResult ListAdApps()
        {
            try
            {
                //Call graph with the acquired token
                using (var httpclient = new HttpClient())
                {
                    httpclient.DefaultRequestHeaders.Add("Authorization", "Bearer " + GetGraphAccessToken());
                    string appresult = httpclient.GetStringAsync("https://graph.microsoft.com/beta/applications").Result;
                    ViewBag.graphresult = JToken.Parse(appresult).ToString(Formatting.Indented);
                };
            }
            catch (Exception ex)
            {
                ViewBag.graphresult = ex.Message;
            }
            return View("GraphResult");
        }

        private string GetGraphAccessToken()
        {
            const string grantType = "client_credentials";
            const string myScopes = "https://graph.microsoft.com/.default";
            string getTokenUrl = $"https://login.microsoftonline.com/{User.Claims.Where(x => x.Type == "http://schemas.microsoft.com/identity/claims/tenantid").FirstOrDefault().Value}/oauth2/v2.0/token";
            string postBody = $"client_id={_configuration["ClientId"]}&scope={myScopes}&client_secret={_configuration["AppSecret"]}&grant_type={grantType}";

            //Get access token
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, getTokenUrl);
            httpRequestMessage.Content = new StringContent(postBody, Encoding.UTF8, "application/x-www-form-urlencoded");
            HttpClient client = new HttpClient();
            HttpResponseMessage httpResponseMessage = client.SendAsync(httpRequestMessage).Result;
            string responseBody = httpResponseMessage.Content.ReadAsStringAsync().Result;
            return JObject.Parse(responseBody).GetValue("access_token").ToString();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
