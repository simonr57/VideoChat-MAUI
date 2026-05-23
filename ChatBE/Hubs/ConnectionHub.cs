using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ChatBE.Hubs
{
    public class CallOffer
    {
        public User Caller { get; set; }
        public User Callee { get; set; }
    }

    public class User
    {
        public string Username { get; set; }
        public string ConnectionId { get; set; }
        public bool InCall { get; set; }
    }

    public class UserCall
    {
        public List<User> Users { get; set; }
    }

    [Authorize(AuthenticationSchemes = "UserClaims")]
    public class ConnectionHub : Hub<IConnectionHub>
    {
        private readonly List<User> _Users;
        private readonly List<UserCall> _UserCalls;
        private readonly List<CallOffer> _CallOffers;
        private readonly List<CallOffer> _CallOffersOffline;

        public ConnectionHub(
            List<User> users,
            List<UserCall> userCalls,
            List<CallOffer> callOffers,
            List<CallOffer> callOffersOffline
        )
        {
            _Users = users;
            _UserCalls = userCalls;
            _CallOffers = callOffers;
            _CallOffersOffline = callOffersOffline;
        }

        public bool IsCalleeUsernameInCallOffers(string calleeUsername)
        {
            var friendsList = Context.User?.FindFirst("Friends")?.Value;
            if (friendsList!.Contains(calleeUsername))
            {
                return _CallOffersOffline.Any(offer =>
                    offer.Callee != null && offer.Callee.Username == calleeUsername
                );
            }
            else
            {
                return false;
            }
        }

        public async Task NotifyCalleeChange(string targetConnectionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, targetConnectionId);
            await Clients.Group(targetConnectionId).ReceiveNotification(targetConnectionId);
        }

        public async Task AddToGroup(string username)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, username);
        }

        public async Task Join(string username)
        {
            _Users.Add(new User { Username = username, ConnectionId = Context.ConnectionId });
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await HangUp();

            _Users.RemoveAll(u => u.ConnectionId == Context.ConnectionId);

            await base.OnDisconnectedAsync(exception);
        }

        public string CallOfflineUser(string targetUsername)
        {
            var targetUser = _Users.SingleOrDefault(u => u.Username == targetUsername);
            if (targetUser != null)
            {
                return targetUser.ConnectionId;
            }
            else
            {
                return "";
            }
        }

        public async Task CallUser(User targetConnectionId)
        {
            var callingUser = _Users.SingleOrDefault(u => u.ConnectionId == Context.ConnectionId);
            var targetUser = _Users.SingleOrDefault(u =>
                u.ConnectionId == targetConnectionId.ConnectionId
            );

            if (targetUser == null)
            {
                _CallOffersOffline.Add(
                    new CallOffer
                    {
                        Caller = callingUser,
                        Callee = new User { Username = targetConnectionId.Username },
                    }
                );

                return;
            }

            if (GetUserCall(targetUser.ConnectionId) != null)
            {
                await Clients.Caller.CallDeclined(
                    targetConnectionId,
                    string.Format("{0} is already in a call.", targetUser.Username)
                );
                return;
            }

            await Clients.Client(targetConnectionId.ConnectionId).IncomingCall(callingUser);

            _CallOffers.Add(new CallOffer { Caller = callingUser, Callee = targetUser });
        }

        public async Task AnswerCall(bool acceptCall, User targetConnectionId)
        {
            var callingUser = _Users.SingleOrDefault(u => u.ConnectionId == Context.ConnectionId);
            var targetUser = _Users.SingleOrDefault(u =>
                u.ConnectionId == targetConnectionId.ConnectionId
            );

            if (callingUser == null)
            {
                return;
            }

            if (targetUser == null)
            {
                await Clients.Caller.CallEnded(
                    targetConnectionId,
                    "The other user in your call has left."
                );
                return;
            }

            if (acceptCall == false)
            {
                await Clients
                    .Client(targetConnectionId.ConnectionId)
                    .CallDeclined(
                        callingUser,
                        string.Format("{0} did not accept your call.", callingUser.Username)
                    );
                return;
            }

            var offerCount = _CallOffers.RemoveAll(c =>
                c.Callee.ConnectionId == callingUser.ConnectionId
                && c.Caller.ConnectionId == targetUser.ConnectionId
            );
            if (offerCount < 1)
            {
                await Clients.Caller.CallEnded(
                    targetConnectionId,
                    string.Format("{0} has already hung up.", targetUser.Username)
                );
                return;
            }

            if (GetUserCall(targetUser.ConnectionId) != null)
            {
                await Clients.Caller.CallDeclined(
                    targetConnectionId,
                    string.Format(
                        "{0} chose to accept someone elses call instead of yours :(",
                        targetUser.Username
                    )
                );
                return;
            }

            _CallOffers.RemoveAll(c => c.Caller.ConnectionId == targetUser.ConnectionId);

            _UserCalls.Add(
                new UserCall
                {
                    Users = new List<User> { callingUser, targetUser },
                }
            );
            await Clients.Client(targetConnectionId.ConnectionId).CallAccepted(callingUser);
        }

        public async Task HangUp()
        {
            var callingUser = _Users.SingleOrDefault(u => u.ConnectionId == Context.ConnectionId);

            if (callingUser == null)
            {
                return;
            }

            var currentCall = GetUserCall(callingUser.ConnectionId);
            if (currentCall != null)
            {
                foreach (
                    var user in currentCall.Users.Where(u =>
                        u.ConnectionId != callingUser.ConnectionId
                    )
                )
                {
                    await Clients
                        .Client(user.ConnectionId)
                        .CallEnded(
                            callingUser,
                            string.Format("{0} has hung up.", callingUser.Username)
                        );
                }
                currentCall.Users.RemoveAll(u => u.ConnectionId == callingUser.ConnectionId);
                if (currentCall.Users.Count < 2)
                {
                    _UserCalls.Remove(currentCall);
                }
            }
            _CallOffers.RemoveAll(c => c.Caller.ConnectionId == callingUser.ConnectionId);
            _Users.RemoveAll(u => u.ConnectionId == Context.ConnectionId);
        }

        public async Task SendSignal(string signal, string targetConnectionId)
        {
            var callingUser = _Users.SingleOrDefault(u => u.ConnectionId == Context.ConnectionId);
            var targetUser = _Users.SingleOrDefault(u => u.ConnectionId == targetConnectionId);
            if (callingUser == null || targetUser == null)
            {
                return;
            }

            var userCall = GetUserCall(callingUser.ConnectionId);

            if (
                userCall != null
                && userCall.Users.Exists(u => u.ConnectionId == targetUser.ConnectionId)
            )
            {
                await Clients.Client(targetConnectionId).ReceiveSignal(callingUser, signal);
            }
        }

        private UserCall GetUserCall(string connectionId)
        {
            var matchingCall = _UserCalls.SingleOrDefault(uc =>
                uc.Users.SingleOrDefault(u => u.ConnectionId == connectionId) != null
            );
            return matchingCall;
        }
    }

    public interface IConnectionHub
    {
        Task UpdateUserList(List<User> userList);
        Task CallAccepted(User acceptingUser);
        Task CallDeclined(User decliningUser, string reason);
        Task IncomingCall(User callingUser);
        Task ReceiveSignal(User signalingUser, string signal);
        Task CallEnded(User signalingUser, string signal);
        Task ReceiveNotification(string targetConnectionId);
    }
}
