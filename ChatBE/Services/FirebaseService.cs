using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using ChatBE.Model;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ChatBE.Services
{
    public class FirebaseService
    {
        private const string firebaseUri = "https://you-url.europe-west1.firebasedatabase.app";
        private const string fcmGoogleAPI =
            "https://fcm.googleapis.com/v1/projects/yourproject/messages:send";
        private const string OK = "Ok";
        private const string NotOK = "Not Ok";
        private const string firebaseAuthDbRole =
            "https://www.googleapis.com/auth/firebase.database";
        private const string firebaseAuthUserRole =
            "https://www.googleapis.com/auth/userinfo.email";
        private const string firebaseAuthMessageRole =
            "https://www.googleapis.com/auth/firebase.messaging";
        private readonly HttpClient _httpClient;
        private string? jsonStr;
        private string? _cachedAccessToken;
        private DateTime _tokenExpiration = DateTime.MinValue;

        public FirebaseService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(firebaseUri);
            jsonStr = configuration["ServiceAccountJson"];
        }

        public async Task<string> SendNotifciation(string deviceToken, string title, string body)
        {
            await AddAuthorizationHeaderAsync();

            var message = new
            {
                message = new
                {
                    token = deviceToken,
                    data = new { title, body },
                    android = new { priority = "high" },
                },
            };

            var response = await _httpClient.PostAsJsonAsync(fcmGoogleAPI, message);
            if (response.IsSuccessStatusCode)
            {
                return OK;
            }
            else
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
                return NotOK;
            }
        }

        private async Task<string> GetAccessTokenAsync()
        {
            if (!string.IsNullOrEmpty(_cachedAccessToken) && DateTime.UtcNow < _tokenExpiration)
            {
                return _cachedAccessToken;
            }

            var credential = await GoogleCredential
                .FromJson(jsonStr)
                .CreateScoped(
                    new[] { firebaseAuthDbRole, firebaseAuthUserRole, firebaseAuthMessageRole }
                )
                .UnderlyingCredential.GetAccessTokenForRequestAsync();

            _cachedAccessToken = credential;
            _tokenExpiration = DateTime.UtcNow.AddMinutes(30);

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

        public async Task UpdateProfilePictureAsync(
            string path,
            string username,
            string base641,
            string base642
        )
        {
            await AddAuthorizationHeaderAsync();

            var updateData = new { base64 = base641, base64sm = base642 };
            var updateContent = new StringContent(
                JsonConvert.SerializeObject(updateData),
                System.Text.Encoding.UTF8,
                "application/json"
            );
            var response = await _httpClient.PatchAsync(
                $"{path}UserNames/{username}.json",
                updateContent
            );
        }

        public async Task<bool> UpdateUserDeviceIdAsync(
            string path,
            string username,
            string deviceId
        )
        {
            await AddAuthorizationHeaderAsync();

            var updateData = new { deviceId = deviceId };
            var updateContent = new StringContent(
                JsonConvert.SerializeObject(updateData),
                System.Text.Encoding.UTF8,
                "application/json"
            );
            var response = await _httpClient.PatchAsync(
                $"{path}UserNames/{username}.json",
                updateContent
            );

            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            return false;
        }

        public async Task<bool> UpdateInviteSentAtAsync(
            string path,
            string username,
            DateTime inviteSentAt
        )
        {
            await AddAuthorizationHeaderAsync();

            var updateData = new { inviteSentAt = inviteSentAt };
            var updateContent = new StringContent(
                JsonConvert.SerializeObject(updateData),
                System.Text.Encoding.UTF8,
                "application/json"
            );
            var response = await _httpClient.PatchAsync(
                $"{path}UserNames/{username}.json",
                updateContent
            );

            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            return false;
        }

        public async Task<List<UserDto>> GetUserFriendsAsync(string path, string userId)
        {
            await AddAuthorizationHeaderAsync();

            var friends = new List<UserDto>();

            // Make the GET request to Firebase for the user's friends list
            var response = await _httpClient.GetAsync(
                $"{path}UserNamesFriendsList/{userId}/friends.json"
            );

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();

                // Deserialize the response into a dictionary where keys are friend IDs and values are Friend objects
                var friendsData = JsonConvert.DeserializeObject<Dictionary<string, UserDto>>(
                    jsonResponse
                );

                // If there are any friends, add them to the list
                if (friendsData != null)
                {
                    foreach (var friend in friendsData)
                    {
                        friends.Add(friend.Value);
                    }
                }
            }
            else
            {
                Console.WriteLine(
                    $"Error fetching friends for user {userId}: {response.StatusCode}"
                );
            }

            return friends;
        }

        public async Task<bool> AreUsersFriendsAsync(string path, string userId1, string userId2)
        {
            await AddAuthorizationHeaderAsync();

            // Get friends list for both users
            var user1Friends = await GetFriendsAsync(path, userId1);
            var user2Friends = await GetFriendsAsync(path, userId2);

            if (user1Friends == null || user2Friends == null)
            {
                return false;
            }
            // Check if user2 is in user1's friends list
            bool isUser2InUser1Friends = user1Friends.Any(friend => friend.Username == userId2);

            // Check if user1 is in user2's friends list
            bool isUser1InUser2Friends = user2Friends.Any(friend => friend.Username == userId1);

            // If both conditions are true, they are friends
            return isUser2InUser1Friends && isUser1InUser2Friends;
        }

        public async Task<List<UserDto>> GetFriendsAsync(string path, string userId)
        {
            await AddAuthorizationHeaderAsync();

            var friends = new List<UserDto>();

            var response = await _httpClient.GetAsync(
                $"{path}UserNamesFriendsList/{userId}/friends.json"
            );

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();

                var friendsData = JsonConvert.DeserializeObject<Dictionary<string, UserDto>>(
                    jsonResponse
                );

                if (friendsData == null)
                {
                    return null;
                }

                foreach (var friend in friendsData)
                {
                    friends.Add(friend.Value);
                }
            }

            return friends;
        }

        public async Task AcceptFriendRequestAsync(
            string path,
            string senderUserId,
            string receiverUserId
        )
        {
            await AddAuthorizationHeaderAsync();

            // Step 1: Update the friend request status to "accepted"
            var updateData = new { status = "Accepted" };
            var updateContent = new StringContent(
                JsonConvert.SerializeObject(updateData),
                System.Text.Encoding.UTF8,
                "application/json"
            );
            var response = await _httpClient.PatchAsync(
                $"{path}FriendRequests/{senderUserId + "_" + receiverUserId}.json",
                updateContent
            );

            if (response.IsSuccessStatusCode)
            {
                // Step 2: Add each user to the other's friends list
                await AddFriendAsync(path, senderUserId, receiverUserId);
                await AddFriendAsync(path, receiverUserId, senderUserId);
            }
            else
            {
                Console.WriteLine($"Error accepting friend request: {response.StatusCode}");
            }
        }

        private async Task AddFriendAsync(string path, string userId, string friendId)
        {
            await AddAuthorizationHeaderAsync();

            var newFriend = new UserDto { Username = friendId, AddedAt = DateTime.UtcNow }; // ISO 8601 format
            var addContent = new StringContent(
                JsonConvert.SerializeObject(newFriend),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            // Add friend under the user's "friends" node
            var response = await _httpClient.PutAsync(
                $"{path}UserNamesFriendsList/{userId}/friends/{friendId}.json",
                addContent
            );

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Friend {friendId} added to {userId}'s friends list.");
            }
            else
            {
                Console.WriteLine(
                    $"Error adding friend {friendId} to {userId}: {response.StatusCode}"
                );
            }
        }

        public async Task AddFriendAsync2Self(string path, string userId, string friendId)
        {
            await AddAuthorizationHeaderAsync();

            var newFriend = new UserDto { Username = friendId, AddedAt = DateTime.UtcNow }; // ISO 8601 format
            var addContent = new StringContent(
                JsonConvert.SerializeObject(newFriend),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            // Add friend under the user's "friends" node
            var response = await _httpClient.PutAsync(
                $"{path}UserNamesFriendsList/{userId}/friends/{friendId}.json",
                addContent
            );

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Friend {friendId} added to {userId}'s friends list.");
            }
            else
            {
                Console.WriteLine(
                    $"Error adding friend {friendId} to {userId}: {response.StatusCode}"
                );
            }
        }

        // Add data to Firebase
        public async Task AddDataAsync<T>(string path, T data)
        {
            await AddAuthorizationHeaderAsync();

            var response = await _httpClient.PutAsJsonAsync($"{path}.json", data);
            response.EnsureSuccessStatusCode();
        }

        public async Task<T> GetDataAsync<T>(string path)
        {
            await AddAuthorizationHeaderAsync();

            return await _httpClient.GetFromJsonAsync<T>($"{path}.json");
        }

        public async Task<bool> IsLastMessageSeen(string path)
        {
            await AddAuthorizationHeaderAsync();

            var response = await _httpClient.GetAsync($"{path}.json");
            response.EnsureSuccessStatusCode(); // Throw on HTTP errors

            string content = await response.Content.ReadAsStringAsync();
            return content != "null"; // "null" means path doesn't exist
        }

        public async Task<Dictionary<string, UserCopy>> GetUserNamesAsync(
            string path,
            string searchString
        )
        {
            await AddAuthorizationHeaderAsync();

            // Fetch all users from the given Firebase path
            var users = await GetDataAsync<Dictionary<string, UserCopy>>(path);

            if (users == null || string.IsNullOrWhiteSpace(searchString))
                return new Dictionary<string, UserCopy>();

            var filteredDictionary = users
                .Where(kvp => kvp.Key.Contains(searchString))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            return filteredDictionary;
        }

        public async Task<Dictionary<string, UserCopy>> GetUserNamesWithProfileAsync(
            string path,
            string searchString
        )
        {
            await AddAuthorizationHeaderAsync();

            var getFriendsArray = searchString.Split(",");
            // Fetch all users from the given Firebase path
            var users = await GetDataAsync<Dictionary<string, User>>(path);

            if (users == null || string.IsNullOrWhiteSpace(searchString))
                return new Dictionary<string, UserCopy>();

            var filteredDictionary = users
                .Where(kvp => getFriendsArray.Contains(kvp.Key))
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => new UserCopy
                    {
                        DeviceId = kvp.Value.DeviceId,
                        Created = kvp.Value.Created,
                        base64 = kvp.Value.base64,
                        base64sm = kvp.Value.base64sm,
                    }
                );

            return filteredDictionary;
        }

        public async Task DeleteDataAsync(string path)
        {
            await AddAuthorizationHeaderAsync();

            var response = await _httpClient.DeleteAsync($"{path}.json");
            response.EnsureSuccessStatusCode();
        }

        // List Incoming Friend Requests (requests where user is the receiver)
        public async Task<List<FriendRequestDto>> ListIncomingFriendRequestsAsync(
            string path,
            string userId
        )
        {
            await AddAuthorizationHeaderAsync();

            var friendRequests = new List<FriendRequestDto>();

            // Get all friend requests from Firebase
            var response = await _httpClient.GetAsync($"{path}.json");

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();

                // Deserialize the response to a dictionary of friend requests
                var requests = JsonConvert.DeserializeObject<Dictionary<string, FriendRequest>>(
                    jsonResponse
                );

                if (requests == null)
                {
                    return new List<FriendRequestDto>();
                }
                // Filter out incoming (received) and pending requests
                foreach (var request in requests)
                {
                    var friendRequest = request.Value;

                    // Check if the current user is the receiver and the request status is pending
                    if (friendRequest.ReceiverUserId == userId && friendRequest.Status == "Pending")
                    {
                        friendRequests.Add(
                            new FriendRequestDto
                            {
                                SenderUserId = friendRequest.SenderUserId,
                                ReceiverUserId = friendRequest.ReceiverUserId,
                                SentAt = friendRequest.SentAt,
                                Status = friendRequest.Status,
                            }
                        );
                    }
                }
            }
            else
            {
                Console.WriteLine("Error retrieving friend requests.");
            }

            return friendRequests;
        }

        public async Task<Dictionary<string, Invite>> GetStaleDeviceIdsAsync(string path)
        {
            await AddAuthorizationHeaderAsync();

            var us = await GetDataAsync<Dictionary<string, Invite>>(path);

            if (us == null)
                return new();

            return us;
        }
    }
}
