using FurniAsiaAddon.Models;
using RestSharp;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FurniAsiaAddon.Services
{
    class LoginService : ILoginService
    {
        public string SendLoginRequest()
        {
            string sessionId = string.Empty;

            ServicePointManager.ServerCertificateValidationCallback += delegate
            {
                return true;
            };

            var url = "https://192.168.1.3:50000/b1s/v1/Login/";

            try
            {
                var client = new RestClient(url);
                var request = new RestRequest(url, Method.POST);

                request.AddHeader("CompanyDB", "SHOP_2023");
                request.AddHeader("UserName", "manager");
                request.AddHeader("Password", "q1w2e3r4T%");

                var response = client.Execute(request);

                Console.WriteLine("Error Message: " + response.ErrorMessage);
                Console.WriteLine("Error Exception: " + response.ErrorException);
                Console.WriteLine("Status Code: " + response.StatusCode);
                Console.WriteLine("Content: " + response.Content);

                foreach (var item in response.Headers)
                {
                    if (item.Name == "Set-Cookie")
                    {
                        sessionId = item.Value.ToString().Split(';')[0] + "; ";
                        sessionId += (item.Value.ToString().Split(',')[1].Split('=')[1] + ";").Replace(" path;", "").TrimEnd(';');
                        Console.WriteLine(sessionId);
                    }
                }
            }
            catch (Exception ex)
            {
                SAPbouiCOM.Framework.Application.SBO_Application.MessageBox(ex.Message);
            }

            return sessionId;
        }

        public async Task<bool> SendPatchRequestAsync(string token, Item item)
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

            string url = $"https://192.168.1.3:50000/b1s/v1/Items('{item.ItemCode}')";

            string jsonData = $@"
    {{
        ""ItemPrices"": [
            {{
                ""PriceList"": 1,
                ""Price"": {item.PackagePrice.ToString(System.Globalization.CultureInfo.InvariantCulture)},
                ""U_PackagePrice"": {item.Price.ToString(System.Globalization.CultureInfo.InvariantCulture)},
                ""Currency"": ""{item.Currency}""
            }}
        ]
    }}";

            Console.WriteLine(jsonData);

            byte[] postDataBytes = Encoding.UTF8.GetBytes(jsonData);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "PATCH";
            request.Headers.Add("Cookie", token);
            request.ContentType = "application/json";
            request.ContentLength = postDataBytes.Length;

            try
            {
                using (Stream requestStream = await request.GetRequestStreamAsync())
                {
                    await requestStream.WriteAsync(postDataBytes, 0, postDataBytes.Length);
                }

                using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
                using (Stream responseStream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(responseStream))
                {
                    string responseContent = await reader.ReadToEndAsync();
                    Console.WriteLine("Status Code: " + (int)response.StatusCode);
                    Console.WriteLine("Response Content: " + responseContent);
                }

                return true;
            }
            catch (WebException ex)
            {
                HttpWebResponse errorResponse = ex.Response as HttpWebResponse;
                if (errorResponse != null)
                {
                    Console.WriteLine("Request failed with status code: " + (int)errorResponse.StatusCode);
                    using (Stream responseStream = errorResponse.GetResponseStream())
                    using (StreamReader reader = new StreamReader(responseStream))
                    {
                        string errorContent = await reader.ReadToEndAsync();
                        Console.WriteLine("Error Content: " + errorContent);

                        SAPbouiCOM.Framework.Application.SBO_Application.StatusBar.SetSystemMessage($"{errorContent}", SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Error);
                        await Task.Delay(1000);
                    }
                }
                else
                {
                    Console.WriteLine("Request failed with an exception: " + ex.Message);
                }
                return false;
            }
        }
    }
}
