using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.SignalR;

namespace ChatBE
{
    public class DictUser
    {
        public string? ConnectionId { get; set; }
    }

    [Authorize(AuthenticationSchemes = "UserClaims")]
    public class ChatHub : Hub
    {
        private static readonly ConcurrentDictionary<string, List<string>> GroupConnections =
            new ConcurrentDictionary<string, List<string>>();
        private static readonly ConcurrentDictionary<string, string> OnlineDictConnIdUsername =
            new ConcurrentDictionary<string, string>();
        private static readonly ConcurrentDictionary<string, string> OnlineDictUsernameConnId =
            new ConcurrentDictionary<string, string>();
        private static readonly ConcurrentDictionary<string, string> OnlineDictConnPublicKeys =
            new ConcurrentDictionary<string, string>();

        public async Task MessageDelivered(string messageId, string senderUsername)
        {
            if (OnlineDictUsernameConnId.TryGetValue(senderUsername, out var connectionId))
            {
                await Clients
                    .Client(connectionId)
                    .SendAsync("MessageDeliveryAcknowledged", messageId);
            }
        }

        /// <summary>
        /// Public key
        /// </summary>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        public void SharePublicKey(string publicKey)
        {
            OnlineDictConnPublicKeys[Context.ConnectionId] = publicKey;
        }

        public string GetConnectionIdByUsername(string username)
        {
            if (OnlineDictUsernameConnId.TryGetValue(username, out var connectionId))
            {
                return connectionId;
            }
            else
            {
                return null!;
            }
        }

        public string GetPublicKey(string username)
        {
            var friendsList = Context.User?.FindFirst("Friends")?.Value;
            if (!friendsList!.Contains(username))
            {
                return "null";
            }

            OnlineDictUsernameConnId.TryGetValue(username, out var connectionId);

            if (connectionId == null)
            {
                return "null";
            }

            // Return the public key for a specific connection ID
            if (OnlineDictConnPublicKeys.TryGetValue(connectionId!, out var publicKey))
            {
                return publicKey;
            }

            return "null";
        }

        /// <summary>
        /// reguser
        /// </summary>
        /// <param name="username"></param>
        public void RegisterUser(string username)
        {
            if (!OnlineDictUsernameConnId.ContainsKey(username))
            {
                OnlineDictConnIdUsername[Context.ConnectionId] = username;
                OnlineDictUsernameConnId[username] = Context.ConnectionId;
            }
        }

        public Task GetOnlineUser(string username)
        {
            var userId = Context.User?.FindFirst("Username")?.Value;

            var friendsList = Context.User?.FindFirst("Friends")?.Value;

            if (!friendsList!.Contains(username))
            {
                throw new HubException("User not authenticated");
            }
            var getTest = OnlineDictUsernameConnId.TryGetValue(username, out var connectionId);

            var targetgroup = GenerateGroupId(userId!, username);

            var conngroup = GroupConnections.TryGetValue(targetgroup, out var list);

            if (list != null && connectionId != null && list.Contains(connectionId))
            {
                return Clients.Caller.SendAsync("ReceiveOnlineUser", true);
            }
            else
            {
                return Clients.Caller.SendAsync("ReceiveOnlineUser", false);
            }
        }

        public async Task SendMessageToGroup(
            string groupId,
            string message1Res,
            string message2Sender
        )
        {
            await Clients
                .Group(groupId)
                .SendAsync(
                    "ReceiveMessage",
                    Context.User?.FindFirst("Username")?.Value,
                    message1Res,
                    message2Sender
                );
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();

            if (httpContext != null)
            {
                var userId = httpContext.User?.FindFirst("Username")?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    Context.Abort();
                    return;
                }
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
                RegisterUser(userId);

                await base.OnConnectedAsync();
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext != null)
            {
                var userId = httpContext.User?.FindFirst("Username")?.Value;

                if (!string.IsNullOrEmpty(userId))
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
                }
                if (exception != null)
                {
                    Console.WriteLine(
                        $"User {userId} disconnected due to an error: {exception.Message}"
                    );
                }
                else
                {
                    Console.WriteLine($"User {userId} disconnected gracefully.");
                }

                if (OnlineDictConnIdUsername.TryRemove(Context.ConnectionId, out var username))
                {
                    OnlineDictUsernameConnId.TryRemove(username, out _);
                    Console.WriteLine(
                        $"User {username} disconnected and was removed from online dictionaries."
                    );
                }
                else
                {
                    Console.WriteLine(
                        $"Connection ID {Context.ConnectionId} was not associated with any username."
                    );
                }

                if (OnlineDictConnPublicKeys.ContainsKey(Context.ConnectionId))
                {
                    OnlineDictConnPublicKeys.TryRemove(Context.ConnectionId, out var c);
                }

                await base.OnDisconnectedAsync(exception);
            }
        }

        public static string GenerateGroupId(string user1Id, string user2Id)
        {
            return string.Compare(user1Id, user2Id) < 0
                ? $"{user1Id}:{user2Id}"
                : $"{user2Id}:{user1Id}";
        }

        public async Task JoinPrivateChat(string friendId)
        {
            var userId = Context.User?.FindFirst("Username")?.Value;
            var friendsList = Context.User?.FindFirst("Friends")?.Value;

            if (string.IsNullOrEmpty(userId) || !friendsList!.Contains(friendId))
            {
                throw new HubException("User not authenticated");
            }

            var groupId = GenerateGroupId(userId, friendId);

            await Groups.AddToGroupAsync(Context.ConnectionId, groupId);

            if (GroupConnections.ContainsKey(groupId))
            {
                GroupConnections[groupId].Add(Context.ConnectionId);
            }
            else
            {
                GroupConnections[groupId] = new List<string> { Context.ConnectionId };
            }

            await Clients.Caller.SendAsync("JoinedGroup", groupId);
        }
    }
}
