using System.IO;
using System.Net.Http.Headers;
using System.Text;
using ChatBE.Model;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Newtonsoft.Json;

namespace ChatBE.Services
{
    public class SyncService
    {
        private const string firebaseUri = "https://you-url.europe-west1.firebasedatabase.app";
        private const string firebaseAuthDbRole =
            "https://www.googleapis.com/auth/firebase.database";
        private const string firebaseAuthUserRole =
            "https://www.googleapis.com/auth/userinfo.email";
        private readonly HttpClient _httpClient;
        private string? jsonStr;
        private string? _cachedAccessToken;
        private DateTime _tokenExpiration = DateTime.MinValue;

        public SyncService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(firebaseUri);
            jsonStr = configuration["ServiceAccountJson"];
        }

        private async Task<string> GetAccessTokenAsync()
        {
            if (!string.IsNullOrEmpty(_cachedAccessToken) && DateTime.UtcNow < _tokenExpiration)
            {
                return _cachedAccessToken;
            }

            var credential = await GoogleCredential
                .FromJson(jsonStr)
                .CreateScoped(new[] { firebaseAuthDbRole, firebaseAuthUserRole })
                .UnderlyingCredential.GetAccessTokenForRequestAsync();

            _cachedAccessToken = credential;
            _tokenExpiration = DateTime.UtcNow.AddMinutes(30); // Assume token is valid for ~1 hour

            return credential;
        }

        private async Task AddAuthorizationHeaderAsync()
        {
            string accessToken = await GetAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                accessToken
            );
        }

        public async Task<Dictionary<string, FireMessage>> GetAllPendingMessages(
            string path,
            string userId
        )
        {
            await AddAuthorizationHeaderAsync();

            var response = await _httpClient.GetAsync(
                $"{path}PendingMessages/{userId}/messages.json"
            );
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(jsonResponse) || jsonResponse == "null")
            {
                Console.WriteLine("No pending messages found.");
                return new Dictionary<string, FireMessage>();
            }

            var messages = System.Text.Json.JsonSerializer.Deserialize<
                Dictionary<string, FireMessage>
            >(jsonResponse);

            return messages!;
        }

        public async Task<FireMessage> GetStory(string path, string userId, string friendUserName)
        {
            await AddAuthorizationHeaderAsync();

            var response = await _httpClient.GetAsync(
                $"{path}PendingBlob/{userId}/messages/{friendUserName}.json"
            );
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(jsonResponse) || jsonResponse == "null")
            {
                Console.WriteLine("No pending messages found.");
                return null!;
            }

            var messages = System.Text.Json.JsonSerializer.Deserialize<FireMessage>(jsonResponse);

            return messages!;
        }

        public async Task<Dictionary<string, FireMessage>> GetAllDeleteMessages(
            string path,
            string userId
        )
        {
            await AddAuthorizationHeaderAsync();

            var response = await _httpClient.GetAsync(
                $"{path}ToDeleteMessages/{userId}/messages.json"
            );
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(jsonResponse) || jsonResponse == "null")
            {
                Console.WriteLine("No pending messages found.");
                return new Dictionary<string, FireMessage>();
            }

            var messages = System.Text.Json.JsonSerializer.Deserialize<
                Dictionary<string, FireMessage>
            >(jsonResponse);

            return messages!;
        }

        public async Task AddPendingMessage(string path, string userId, FireMessage fireMessage)
        {
            await AddAuthorizationHeaderAsync();

            var json = System.Text.Json.JsonSerializer.Serialize(fireMessage);
            var contentData = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync(
                $"{path}PendingMessages/{userId}/messages/{fireMessage.Id}.json",
                contentData
            );

            response.EnsureSuccessStatusCode();

            Console.WriteLine("Message added successfully.");
        }

        public async Task AddPendingBlob(string path, string userId, FireMessage fireMessage)
        {
            await AddAuthorizationHeaderAsync();

            var json = System.Text.Json.JsonSerializer.Serialize(fireMessage);
            var contentData = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync(
                $"{path}PendingBlob/{userId}/messages/{fireMessage.Sender}.json",
                contentData
            );

            response.EnsureSuccessStatusCode();

            Console.WriteLine("Message added successfully.");
        }

        public async Task AddToDeleteMessage(string path, string userId, FireMessage fireMessage)
        {
            await AddAuthorizationHeaderAsync();

            var json = System.Text.Json.JsonSerializer.Serialize(fireMessage);
            var contentData = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync(
                $"{path}ToDeleteMessages/{userId}/messages/{fireMessage.Id}.json",
                contentData
            );

            response.EnsureSuccessStatusCode();

            Console.WriteLine("Message added successfully.");
        }

        public async Task DeleteDataAsync(string path)
        {
            await AddAuthorizationHeaderAsync();

            var response = await _httpClient.DeleteAsync($"{path}.json");
            response.EnsureSuccessStatusCode();
        }
    }
}
