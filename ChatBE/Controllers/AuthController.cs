using System.IO;
using System.Reflection;
using System.Security.Claims;
using ChatBE.Model;
using ChatBE.Services;
using ChatBE.Util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ChatBE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private const string notOK = "Not Ok";
        private readonly FirebaseService _firebaseService;
        private readonly IPasswordHasher<string> _passwordHasher;
        private readonly IGenerateJWT _generateJWT;

        public AuthController(
            FirebaseService firebaseService,
            IPasswordHasher<string> passwordHasher,
            IGenerateJWT generateJWT
        )
        {
            _firebaseService = firebaseService;
            _passwordHasher = passwordHasher;
            _generateJWT = generateJWT;
        }

        [HttpPost]
        [Route("RefreshToken")]
        [Authorize(AuthenticationSchemes = "UserClaims")]
        public IActionResult RefreshToken([FromBody] PasswordLogin login)
        {
            string reset = HttpContext.Request.Query["reset"].ToString();

            IActionResult response = Unauthorized("Error while refreshing the token");

            var identity = HttpContext.User.Identity as ClaimsIdentity;
            var identityJWT = HttpContext.Request.Headers.Authorization.ToString();
            string token = identityJWT.Contains("Bearer") ? identityJWT[7..] : "";

            var getUserClaim = identity?.Claims;

            if (getUserClaim != null)
            {
                var userName = getUserClaim.First(c => c.Type == "Username").Value;
                var friends = getUserClaim.First(c => c.Type == "Friends").Value;
                if (identityJWT != null && JwtHelper.ValidateJwtToken(token))
                {
                    var tokenTuple = _generateJWT.GenerateJSONWebToken(friends, userName);

                    response = Ok(tokenTuple.Item1);
                }
            }

            return response;
        }

        [HttpPost]
        [Route("RefreshToken2")]
        [Authorize(AuthenticationSchemes = "UserClaims")]
        public async Task<IActionResult> RefreshToken2([FromBody] PasswordLogin login)
        {
            string reset = HttpContext.Request.Query["reset"].ToString();

            IActionResult response = Unauthorized("Error while refreshing the token");

            var identity = HttpContext.User.Identity as ClaimsIdentity;
            var identityJWT = HttpContext.Request.Headers.Authorization.ToString();
            string token = identityJWT.Contains("Bearer") ? identityJWT[7..] : "";

            var getUserClaim = identity?.Claims;

            if (getUserClaim != null)
            {
                var userName = getUserClaim.First(c => c.Type == "Username").Value;

                var friendsList = await _firebaseService.GetUserFriendsAsync("dB/", userName);
                var query = string.Join(",", friendsList.Select(a => a.Username));

                var friends = query;
                if (identityJWT != null && JwtHelper.ValidateJwtToken(token))
                {
                    var tokenTuple = _generateJWT.GenerateJSONWebToken(friends, userName);

                    response = Ok(tokenTuple.Item1);
                }
            }

            return response;
        }

        [Authorize(AuthenticationSchemes = "UserClaims")]
        [HttpPost]
        [Route("DeleteAccount")]
        public async Task<bool> DeleteAccount([FromBody] SearchModel requestdto)
        {
            var usersession = HttpContext.User.Identity as ClaimsIdentity;
            var senderUsername = usersession?.Claims.First(c => c.Type == "Username").Value!;

            if (senderUsername != null)
            {
                //delete pedning
                await _firebaseService.DeleteDataAsync("dB/PendingMessages/" + senderUsername);

                //delete pedning delete
                await _firebaseService.DeleteDataAsync("dB/ToDeleteMessages/" + senderUsername);

                //delete friends list
                await _firebaseService.DeleteDataAsync("dB/UserNamesFriendsList/" + senderUsername);

                //delete username
                await _firebaseService.DeleteDataAsync("dB/UserNames/" + senderUsername);

                return true;
            }

            return false;
        }

        [Authorize(AuthenticationSchemes = "UserClaims")]
        [HttpPost]
        [Route("DeleteAllPendingMessages")]
        public async Task<bool> DeleteAllPendingMessages([FromBody] SearchModel requestdto)
        {
            var usersession = HttpContext.User.Identity as ClaimsIdentity;

            var senderUsername = usersession?.Claims.First(c => c.Type == "Username").Value!;

            if (senderUsername != null)
            {
                await _firebaseService.DeleteDataAsync("dB/PendingMessages/" + senderUsername);
                return true;
            }

            return false;
        }

        [Authorize(AuthenticationSchemes = "UserClaims")]
        [HttpPost]
        [Route("SendNotification")]
        public async Task<string> SendNotification([FromBody] SearchModel2 requestdto)
        {
            var usersession = HttpContext.User.Identity as ClaimsIdentity;

            var senderUsername = usersession?.Claims.First(c => c.Type == "Username").Value!;

            try
            {
                var t = await _firebaseService.SendNotifciation(
                    requestdto.Username1,
                    senderUsername,
                    requestdto.Username2
                );
                return t;
            }
            catch (Exception)
            {
                return notOK;
            }
        }

        [Authorize(AuthenticationSchemes = "UserClaims")]
        [HttpPost]
        [Route("SendNotificationInvite")]
        public async Task SendNotificationInvite([FromBody] SearchModel2 requestdto)
        {
            await _firebaseService.SendNotifciation(
                requestdto.Username1,
                requestdto.Username2,
                requestdto.Username2
            );
        }

        [Authorize(AuthenticationSchemes = "UserClaims")]
        [HttpPost]
        [Route("GetStaleDeviceIdsAsync")]
        public async Task<Dictionary<string, Invite>> GetStaleDeviceIdsAsync(
            [FromBody] SearchModel requestdto
        )
        {
            var t = await _firebaseService.GetStaleDeviceIdsAsync("dB/Invites");
            return t;
        }

        [Authorize(AuthenticationSchemes = "UserClaims")]
        [HttpPost]
        [Route("DeleteFriendFromList")]
        public async Task<bool> DeleteFriendFromList([FromBody] SearchModel requestdto)
        {
            var usersession = HttpContext.User.Identity as ClaimsIdentity;

            var senderUsername = usersession?.Claims.First(c => c.Type == "Username").Value!;

            var friendsList = usersession?.Claims.First(c => c.Type == "Friends").Value!;

            if (
                senderUsername != null
                && requestdto.Username != null
                && friendsList.Contains(requestdto.Username)
            )
            {
                await _firebaseService.DeleteDataAsync(
                    "dB/UserNamesFriendsList/" + senderUsername + "/friends/" + requestdto.Username
                );

                await _firebaseService.DeleteDataAsync(
                    "dB/UserNamesFriendsList/" + requestdto.Username + "/friends/" + senderUsername
                );

                return true;
            }

            return false;
        }

        [Authorize(AuthenticationSchemes = "UserClaims")]
        [HttpPost]
        [Route("DeleteAllDeletes")]
        public async Task<bool> DeleteAllDeletes([FromBody] SearchModel requestdto)
        {
            var usersession = HttpContext.User.Identity as ClaimsIdentity;

            var senderUsername = usersession?.Claims.First(c => c.Type == "Username").Value!;

            if (senderUsername != null)
            {
                await _firebaseService.DeleteDataAsync("dB/ToDeleteMessages/" + senderUsername);
                return true;
            }

            return false;
        }

        [Authorize(AuthenticationSchemes = "UserClaims")]
        [HttpPost]
        [Route("UpdateDeviceId")]
        public async Task<bool> UpdateDeviceId([FromBody] SearchModel requestdto)
        {
            var usersession = HttpContext.User.Identity as ClaimsIdentity;

            var senderUsername = usersession?.Claims.First(c => c.Type == "Username").Value!;

            return await _firebaseService.UpdateUserDeviceIdAsync(
                "dB/",
                senderUsername,
                requestdto.Username
            );
        }

        [Authorize(AuthenticationSchemes = "UserClaims")]
        [HttpPost]
        [Route("UpdateInviteSentAtAsync")]
        public async Task<bool> UpdateInviteSentAtAsync([FromBody] SearchModel requestdto)
        {
            var usersession = HttpContext.User.Identity as ClaimsIdentity;

            var senderUsername = usersession?.Claims.First(c => c.Type == "Username").Value!;

            await _firebaseService.AddDataAsync(
                $"dB/Invites/{senderUsername}",
                new Invite
                {
                    SentAt = DateTime.UtcNow,
                    DeviceId = await _firebaseService.GetDataAsync<string>(
                        $"dB/UserNames/{senderUsername}/deviceId"
                    ),
                }
            );

            return true;
        }

        [Authorize(AuthenticationSchemes = "UserClaims")]
        [HttpPost]
        [Route("UpdateProfilePictureAsync")]
        public async Task UpdateProfilePictureAsync([FromBody] SearchModel2 sender)
        {
            var usersession = HttpContext.User.Identity as ClaimsIdentity;

            var reciver = usersession?.Claims.First(c => c.Type == "Username").Value!;

            await _firebaseService.UpdateProfilePictureAsync(
                "dB/",
                reciver,
                sender.Username1,
                sender.Username2
            );
        }

        [Authorize(AuthenticationSchemes = "UserClaims")]
        [HttpPost]
        [Route("GetProfilePictureAsync")]
        public async Task<string?> GetProfilePictureAsync([FromBody] SearchModel sender)
        {
            var usersession = HttpContext.User.Identity as ClaimsIdentity;

            var reciver = usersession?.Claims.First(c => c.Type == "Username").Value!;

            var result = await _firebaseService.GetDataAsync<User>($"dB/UserNames/{reciver}");
            if (result == null)
            {
                return null;
            }
            return result.base64;
        }

        [Authorize(AuthenticationSchemes = "UserClaims")]
        [HttpPost]
        [Route("GetProfilePictureSMAsync")]
        public async Task<string?> GetProfilePictureSMAsync([FromBody] SearchModel sender)
        {
            var usersession = HttpContext.User.Identity as ClaimsIdentity;

            var reciver = usersession?.Claims.First(c => c.Type == "Username").Value!;

            var client = await _firebaseService.GetDataAsync<User>($"dB/UserNames/{reciver}");

            if (client == null)
            {
                return null;
            }
            return client.base64sm;
        }

        [Authorize(AuthenticationSchemes = "UserClaims")]
        [HttpPost]
        [Route("GetUserNamesWithProfileAsync")]
        public async Task<Dictionary<string, UserCopy>?> GetUserNamesWithProfileAsync(
            [FromBody] SearchModel sender
        )
        {
            var result = await _firebaseService.GetUserNamesWithProfileAsync(
                $"dB/UserNames",
                sender.Username
            );
            if (result == null)
            {
                return null;
            }
            return result;
        }

        [Authorize(AuthenticationSchemes = "UserClaims")]
        [HttpPost]
        [Route("GetUserCharIdByUsernameAsync")]
        public async Task<int> GetUserCharIdByUsernameAsync([FromBody] SearchModel sender)
        {
            var usersession = HttpContext.User.Identity as ClaimsIdentity;

            var reciver = usersession?.Claims.First(c => c.Type == "Username").Value!;

            var result = await _firebaseService.GetDataAsync<int>(
                $"dB/UserNames/{sender.Username}/charId"
            );

            return result;
        }

        [Authorize(AuthenticationSchemes = "UserClaims")]
        [HttpPost]
        [Route("GetPendingMessageIdsAsync")]
        public async Task<dynamic> GetPendingMessageIdsAsync([FromBody] SearchModel sender)
        {
            var result = await _firebaseService.IsLastMessageSeen(
                $"dB/PendingMessages/{sender.Username}"
            );

            return result;
        }

        [Authorize(AuthenticationSchemes = "UserClaims")]
        [HttpPost]
        [Route("GetUserDeviceIdByUsernameAsync")]
        public async Task<string> GetUserDeviceIdByUsernameAsync([FromBody] SearchModel sender)
        {
            var result = await _firebaseService.GetDataAsync<string>(
                $"dB/UserNames/{sender.Username}/deviceId"
            );
            return result;
        }

        [Authorize(AuthenticationSchemes = "UserClaims")]
        [HttpPost]
        [Route("GetUserFriendsAsync")]
        public async Task<List<UserDto>> GetUserFriendsAsync([FromBody] SearchModel sender)
        {
            var usersession = HttpContext.User.Identity as ClaimsIdentity;

            var reciver = usersession?.Claims.First(c => c.Type == "Username").Value!;

            return await _firebaseService.GetUserFriendsAsync("dB/", reciver);
        }

        [Authorize(AuthenticationSchemes = "UserClaims")]
        [HttpPost]
        [Route("AcceptFriendRequest")]
        public async Task AcceptFriendRequest([FromBody] SearchModel sender)
        {
            var usersession = HttpContext.User.Identity as ClaimsIdentity;

            var reciver = usersession?.Claims.First(c => c.Type == "Username").Value!;

            await _firebaseService.AcceptFriendRequestAsync("dB/", sender.Username, reciver);
        }

        [Authorize(AuthenticationSchemes = "UserClaims")]
        [HttpPost]
        [Route("DeleteFriendRequest")]
        public async Task DeleteFriendRequest([FromBody] SearchModel sender)
        {
            var usersession = HttpContext.User.Identity as ClaimsIdentity;

            var reciver = usersession?.Claims.First(c => c.Type == "Username").Value!;

            await _firebaseService.DeleteDataAsync(
                $"dB/FriendRequests/{sender.Username + "_" + reciver}"
            );
        }

        [Authorize(AuthenticationSchemes = "UserClaims")]
        [HttpPost]
        [Route("ListIncommingRequests")]
        public async Task<List<FriendRequestDto>> ListIncommingRequests(
            [FromBody] SearchModel search
        )
        {
            var usersession = HttpContext.User.Identity as ClaimsIdentity;

            var username = usersession?.Claims.First(c => c.Type == "Username").Value!;

            var result = await _firebaseService.ListIncomingFriendRequestsAsync(
                $"dB/FriendRequests",
                username
            );
            if (result == null)
            {
                return null;
            }
            return result;
        }

        [Authorize(AuthenticationSchemes = "UserClaims")]
        [HttpPost]
        [Route("ReportAbuse")]
        public async Task<string?> ReportAbuse([FromBody] SearchModel2 requestdto)
        {
            var usersession = HttpContext.User.Identity as ClaimsIdentity;

            var senderUsername = usersession?.Claims.First(c => c.Type == "Username").Value!;

            await _firebaseService.AddDataAsync(
                $"dB/FriendReport/{senderUsername + '_' + requestdto.Username1}",
                new Report
                {
                    WhenAt = DateTime.Now,
                    Reporter = senderUsername,
                    Reporting = requestdto.Username1,
                    Issue = requestdto.Username2,
                }
            );

            return "Sent";
        }

        [Authorize(AuthenticationSchemes = "UserClaims")]
        [HttpPost]
        [Route("SendFriendRequest")]
        public async Task<string?> SendFriendRequest([FromBody] SearchModel requestdto)
        {
            var usersession = HttpContext.User.Identity as ClaimsIdentity;

            var senderUsername = usersession?.Claims.First(c => c.Type == "Username").Value!;

            var result1 = await _firebaseService.GetDataAsync<dynamic>(
                $"dB/FriendRequests/{senderUsername + '_' + requestdto.Username}"
            );
            var result2 = await _firebaseService.GetDataAsync<dynamic>(
                $"dB/FriendRequests/{requestdto.Username}" + "_" + senderUsername
            );
            if (result1 != null || result2 != null)
            {
                return null;
            }

            await _firebaseService.AddDataAsync(
                $"dB/FriendRequests/{senderUsername + '_' + requestdto.Username}",
                new FriendRequest
                {
                    SentAt = DateTime.Now,
                    RequestId = senderUsername + '_' + requestdto.Username,
                    ReceiverUserId = requestdto.Username,
                    SenderUserId = senderUsername,
                    Status = "Pending",
                }
            );

            return "Sent";
        }

        [HttpPost]
        [Route("SendFriendRequestOwn")]
        public async Task<string?> SendFriendRequestOwn([FromBody] SearchModel requestdto)
        {
            await _firebaseService.AddFriendAsync2Self("dB/", requestdto.Username, "Testing");

            return "Sent";
        }

        [Authorize(AuthenticationSchemes = "UserClaims")]
        [HttpPost]
        [Route("AllUsersBySearch")]
        public async Task<Dictionary<string, UserCopy>?> AllUsersBySearch(
            [FromBody] SearchModel search
        )
        {
            var result = await _firebaseService.GetUserNamesAsync($"dB/UserNames", search.Username);
            if (result == null)
            {
                return null;
            }
            return result;
        }

        [HttpPost]
        [Route("CheckUser")]
        public async Task<string?> CheckUser([FromBody] PasswordLogin login)
        {
            var result = await _firebaseService.GetDataAsync<User>(
                $"dB/UserNames/{login.Username}"
            );
            if (result == null)
            {
                return null;
            }
            return result.Created.ToString();
        }

        [HttpPost]
        [Route("Login")]
        public async Task<IActionResult> Login([FromBody] PasswordLoginRegister login)
        {
            IActionResult response = Unauthorized("Error while logging in");

            if (login.Username == null)
            {
                return response;
            }

            var result = await _firebaseService.GetDataAsync<User>(
                $"dB/UserNames/{login.Username}"
            );

            if (result == null)
            {
                await _firebaseService.AddDataAsync(
                    $"dB/UserNames/{login.Username}",
                    new User
                    {
                        DeviceId = login.DeviceId,
                        Created = DateTime.Now,
                        HashedPW = _passwordHasher.HashPassword(login.HashedPW, login.HashedPW),
                        base64sm =
                            "iVBORw0KGgoAAAANSUhEUgAAAGQAAABkCAIAAAD/gAIDAAAA6ElEQVR4nO3QwQ3AIBDAsNLJb3RWIC+EZE8QZc3Mx5n/dsBLzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCjbLZgJIjFtsAQAAAABJRU5ErkJggg==",
                        base64 =
                            "iVBORw0KGgoAAAANSUhEUgAAAGQAAABkCAIAAAD/gAIDAAAA6ElEQVR4nO3QwQ3AIBDAsNLJb3RWIC+EZE8QZc3Mx5n/dsBLzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCswKzArMCjbLZgJIjFtsAQAAAABJRU5ErkJggg==",
                    }
                );
                var tokenTuple = _generateJWT.GenerateJSONWebToken("", login.Username);

                var loggedIn = new LoggedInResponse
                {
                    username = login.Username,
                    jwttoken = tokenTuple.Item1,
                    expire = tokenTuple.Item2,
                };

                response = Ok(loggedIn.jwttoken);
            }
            else
            {
                var checkPassword = _passwordHasher.VerifyHashedPassword(
                    login.Username,
                    result.HashedPW,
                    login.HashedPW
                );

                if (PasswordVerificationResult.Success == checkPassword)
                {
                    var friendsList = await _firebaseService.GetUserFriendsAsync(
                        "dB/",
                        login.Username
                    );
                    var getFriends = string.Join(",", friendsList.Select(a => a.Username));

                    var tokenTuple = _generateJWT.GenerateJSONWebToken(
                        getFriends ?? "",
                        login.Username
                    );

                    var loggedIn = new LoggedInResponse
                    {
                        username = login.Username,
                        jwttoken = tokenTuple.Item1,
                        expire = tokenTuple.Item2,
                    };

                    response = Ok(loggedIn.jwttoken);
                }
            }
            return response;
        }
    }
}
