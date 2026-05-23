using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using ChatApp.Models;
using ChatApp.Utilities;
using Newtonsoft.Json;

namespace ChatApp.External
{
    public class FirebaseService
    {
        private readonly HttpClient _httpClient;
        private readonly string jwt = "_jwttoken";
        private readonly string bearer = "Bearer";
        private readonly string contentType = "application/json";
        private readonly string notOK = "not ok";

        public FirebaseService(HttpClient httpClient)
        {
            var _jwt = LocalDbExtensions.RetrieveSecureString(jwt);
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                bearer,
                _jwt
            );
        }

        public async Task<List<UserDto>?> PostDataAsyncReturnListOfUsers(string input, string url2)
        {
            string url = Configuration.BackendURL + url2;
            var data = new { Username = input };
            string jsonData = System.Text.Json.JsonSerializer.Serialize(data);
            var content = new StringContent(jsonData, Encoding.UTF8, contentType);

            try
            {
                HttpResponseMessage response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    List<UserDto>? result = JsonConvert.DeserializeObject<List<UserDto>>(
                        responseBody
                    );

                    return result;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return null;
            }
        }

        public async Task<List<FriendRequest>?> PostDataAsyncReturnListOfRequests(
            string input,
            string url2
        )
        {
            string url = Configuration.BackendURL + url2;
            var data = new { Username = input };
            string jsonData = System.Text.Json.JsonSerializer.Serialize(data);
            var content = new StringContent(jsonData, Encoding.UTF8, contentType);

            try
            {
                HttpResponseMessage response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    List<FriendRequest>? result = JsonConvert.DeserializeObject<
                        List<FriendRequest>
                    >(responseBody);
                    return result;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return null;
            }
        }

        public async Task<Dictionary<string, UserCopy>?> PostDataAsyncReturnDict(
            string input,
            string url2
        )
        {
            string url = Configuration.BackendURL + url2;
            var data = new { Username = input };
            string jsonData = System.Text.Json.JsonSerializer.Serialize(data);
            var content = new StringContent(jsonData, Encoding.UTF8, contentType);

            try
            {
                HttpResponseMessage response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    var t = await response.Content.ReadFromJsonAsync<
                        Dictionary<string, UserCopy>
                    >();
                    return t;
                }
                else
                {
                    return new Dictionary<string, UserCopy>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return new Dictionary<string, UserCopy>();
            }
        }

        public async Task<List<string>?> PostDataAsyncReturnList(string input, string url2)
        {
            string url = Configuration.BackendURL + url2;
            var data = new { Username = input };
            string jsonData = System.Text.Json.JsonSerializer.Serialize(data);
            var content = new StringContent(jsonData, Encoding.UTF8, contentType);

            try
            {
                HttpResponseMessage response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();

                    List<string>? result = JsonConvert.DeserializeObject<List<string>>(
                        responseBody
                    );
                    return result;
                }
                else
                {
                    return new List<string>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return new List<string>();
            }
        }

        public async Task<string> PostDataAsync(string input, string url2)
        {
            string url = Configuration.BackendURL + url2;
            var data = new { Username = input };
            string jsonData = System.Text.Json.JsonSerializer.Serialize(data);
            var content = new StringContent(jsonData, Encoding.UTF8, contentType);

            try
            {
                HttpResponseMessage response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Success: {responseBody}");
                    return responseBody;
                }
                else
                {
                    return notOK;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return notOK;
            }
        }

        public async Task<string> PostDataTwoAsync(string input1, string input2, string url2)
        {
            string url = Configuration.BackendURL + url2;
            var data = new { Username1 = input1, Username2 = input2 };
            string jsonData = System.Text.Json.JsonSerializer.Serialize(data);
            var content = new StringContent(jsonData, Encoding.UTF8, contentType);

            try
            {
                HttpResponseMessage response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Success: {responseBody}");
                    return responseBody;
                }
                else
                {
                    return "not ok";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return "not ok";
            }
        }

        public async Task<Dictionary<string, UserCopy>?> GetUserNamesWithProfileAsync(
            string input,
            string url2
        )
        {
            string url = Configuration.BackendURL + url2;
            var data = new { Username = input };
            string jsonData = System.Text.Json.JsonSerializer.Serialize(data);
            var content = new StringContent(jsonData, Encoding.UTF8, contentType);

            try
            {
                HttpResponseMessage response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    var t = await response.Content.ReadFromJsonAsync<
                        Dictionary<string, UserCopy>
                    >();
                    return t;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return null;
            }
        }

        public async Task<Dictionary<string, Invite>?> GetInvitesAsync(string input, string url2)
        {
            string url = Configuration.BackendURL + url2;
            var data = new { Username = input };
            string jsonData = System.Text.Json.JsonSerializer.Serialize(data);
            var content = new StringContent(jsonData, Encoding.UTF8, contentType);

            try
            {
                HttpResponseMessage response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    var t = await response.Content.ReadFromJsonAsync<Dictionary<string, Invite>>();
                    return t;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return null;
            }
        }

        public async Task<FireMessage?> GetStoryAsync(string input, string url2)
        {
            string url = Configuration.BackendURL + url2;
            var data = new { Username = input };
            string jsonData = System.Text.Json.JsonSerializer.Serialize(data);
            var content = new StringContent(jsonData, Encoding.UTF8, contentType);

            try
            {
                HttpResponseMessage response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    var t = await response.Content.ReadFromJsonAsync<FireMessage>();
                    return t;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return null;
            }
        }
    }
}
