using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Net.Http.Json;
using Newtonsoft.Json;

namespace HasheousClient.WebApp
{
    public static class HttpHelper
    {
        public static string BaseUri 
        {
            get
            {
                return apiBasicUri;
            }
            set
            {
                client.BaseAddress = new Uri(value);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            } 
        }

        private static string apiBasicUri { get; set; }

        private static HttpClient client = new HttpClient();

        public static async Task<T> Post<T>(string url, object contentValue)
        {
            var jsonContent = JsonContent.Create(contentValue);
            await jsonContent.LoadIntoBufferAsync();
            var response = await client.PostAsync(url, jsonContent);
            response.EnsureSuccessStatusCode();

            // Deserialize the updated product from the response body.
            var resultStr = await response.Content.ReadAsStringAsync();
            var resultObject = JsonConvert.DeserializeObject<T>(resultStr);
            return resultObject;
        }

        // public static async Task Put<T>(string url, T stringValue)
        // {
        //     Client.BaseAddress = new Uri(apiBasicUri);
        //     var content = new StringContent(JsonConvert.SerializeObject(stringValue), Encoding.UTF8, "application/json");
        //     var result = await Client.PutAsync(url, content);
        //     result.EnsureSuccessStatusCode();
        // }

        // public static async Task<T> Get<T>(string url)
        // {
        //     Client.BaseAddress = new Uri(apiBasicUri);
        //     var result = await Client.GetAsync(url);
        //     result.EnsureSuccessStatusCode();
        //     string resultContentString = await result.Content.ReadAsStringAsync();
        //     T resultContent = JsonConvert.DeserializeObject<T>(resultContentString);
        //     return resultContent;
        // }

        // public static async Task Delete(string url)
        // {
        //     Client.BaseAddress = new Uri(apiBasicUri);
        //     var result = await Client.DeleteAsync(url);
        //     result.EnsureSuccessStatusCode();
        // }
    }
}