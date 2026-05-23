using System.Net.Http.Headers;
using System.Net.Http.Json;
using ChatApp.Models;
using ChatApp.Utilities;
using Newtonsoft.Json;

namespace ChatApp.External
{
    public class SyncClientService
    {
        private readonly HttpClient _httpClient;
        private readonly string jwt = "_jwttoken";
        private readonly string bearer = "Bearer";

        public SyncClientService(HttpClient httpClient)
        {
            var _jwt = LocalDbExtensions.RetrieveSecureString(jwt);

            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                bearer,
                _jwt
            );
        }

        public async Task<Dictionary<string, FireMessage>?> SyncFireData(string url2)
        {
            string url = Configuration.BackendURL + url2;

            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();

                    Dictionary<string, FireMessage>? result = JsonConvert.DeserializeObject<
                        Dictionary<string, FireMessage>
                    >(responseBody);
                    return result;
                }
                else
                {
                    return new Dictionary<string, FireMessage>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return new Dictionary<string, FireMessage>();
            }
        }

        public async Task<string> DeleteFireMessage(string url2)
        {
            string url = Configuration.BackendURL + url2;

            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    return responseBody;
                }
                else
                {
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return string.Empty;
            }
        }

        public async Task<bool> PostPendingMessage(FireMessage message, string url2)
        {
            string url = Configuration.BackendURL + url2;

            try
            {
                HttpResponseMessage response = await _httpClient.PostAsJsonAsync(url, message);

                if (response.IsSuccessStatusCode)
                {
                    bool result = await response.Content.ReadFromJsonAsync<bool>();
                    Console.WriteLine($"Message successfully sent: {result}");
                    return result;
                }
                else
                {
                    Console.WriteLine(
                        $"Failed to send message. Status Code: {response.StatusCode}"
                    );
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
            return false;
        }
    }
}
