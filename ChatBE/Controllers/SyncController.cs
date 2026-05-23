using System.Reflection;
using System.Security.Claims;
using ChatBE.Model;
using ChatBE.Services;
using ChatBE.Util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ChatBE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SyncController : ControllerBase
    {
        private readonly SyncService _syncService;
        private readonly IGenerateJWT _generateJWT;

        public SyncController(SyncService syncService, IGenerateJWT generateJWT)
        {
            _syncService = syncService;
            _generateJWT = generateJWT;
        }

        [Authorize(AuthenticationSchemes = "UserClaims")]
        [HttpGet]
        [Route("DeleteFriendMessage/{Username}/{Id}")]
        public async Task<bool> DeleteFriendMessage(string Username, string Id)
        {
            var usersession = HttpContext.User.Identity as ClaimsIdentity;

            var username = usersession?.Claims.First(c => c.Type == "Username").Value!;
            var friendsList = usersession?.Claims.First(c => c.Type == "Friends").Value!;

            if (friendsList.Contains(Username))
            {
                await _syncService.DeleteDataAsync($"dB/PendingMessages/{Username}/messages/{Id}");

                return true;
            }
            else
            {
                return false;
            }
        }

        [Authorize(AuthenticationSchemes = "UserClaims")]
        [HttpGet]
        [Route("SyncMessages")]
        public async Task<Dictionary<string, FireMessage>> SyncMessages()
        {
            var usersession = HttpContext.User.Identity as ClaimsIdentity;

            var username = usersession?.Claims.First(c => c.Type == "Username").Value!;

            var result = await _syncService.GetAllPendingMessages($"dB/", username);
            if (result == null)
            {
                return new Dictionary<string, FireMessage>();
            }
            return result;
        }

        [Authorize(AuthenticationSchemes = "UserClaims")]
        [HttpPost]
        [Route("GetStory")]
        public async Task<FireMessage> GetStory([FromBody] SearchModel requestdto)
        {
            var usersession = HttpContext.User.Identity as ClaimsIdentity;

            var username = usersession?.Claims.First(c => c.Type == "Username").Value!;

            var result = await _syncService.GetStory($"dB/", username, requestdto.Username);
            if (result == null)
            {
                return new FireMessage
                {
                    Id = "noId",
                    Sender = "noName",
                    SentDate = DateTime.Now,
                    Text = "noText",
                };
            }
            return result;
        }

        [Authorize(AuthenticationSchemes = "UserClaims")]
        [HttpGet]
        [Route("SyncDelete")]
        public async Task<Dictionary<string, FireMessage>> SyncDelete()
        {
            var usersession = HttpContext.User.Identity as ClaimsIdentity;

            var username = usersession?.Claims.First(c => c.Type == "Username").Value!;

            var result = await _syncService.GetAllDeleteMessages($"dB/", username);
            if (result == null)
            {
                return new Dictionary<string, FireMessage>();
            }
            return result;
        }

        [Authorize(AuthenticationSchemes = "UserClaims")]
        [HttpPost]
        [Route("AddPendingMessage/{target}")]
        public async Task<bool> AddPendingMessage(
            string target,
            [FromBody] Model.FireMessage message
        )
        {
            var usersession = HttpContext.User.Identity as ClaimsIdentity;

            var sender = usersession?.Claims.First(c => c.Type == "Username").Value!;

            message.Sender = sender;

            var friendsList = usersession?.Claims.First(c => c.Type == "Friends").Value!;
            if (friendsList.Contains(target))
            {
                await _syncService.AddPendingMessage($"dB/", target, message);
                return true;
            }
            else
            {
                return false;
            }
        }

        [Authorize(AuthenticationSchemes = "UserClaims")]
        [HttpPost]
        [Route("AddPendingBlob/{target}")]
        public async Task<bool> AddPendingBlob(string target, [FromBody] Model.FireMessage message)
        {
            var usersession = HttpContext.User.Identity as ClaimsIdentity;

            var sender = usersession?.Claims.First(c => c.Type == "Username").Value!;

            message.Sender = sender;

            var friendsList = usersession?.Claims.First(c => c.Type == "Friends").Value!;
            if (friendsList.Contains(target))
            {
                await _syncService.AddPendingBlob($"dB/", target, message);
                return true;
            }
            else
            {
                return false;
            }
        }

        [Authorize(AuthenticationSchemes = "UserClaims")]
        [HttpPost]
        [Route("AddToDeleteMessage/{target}")]
        public async Task<bool> AddToDeleteMessage(
            string target,
            [FromBody] Model.FireMessage message
        )
        {
            var usersession = HttpContext.User.Identity as ClaimsIdentity;

            var sender = usersession?.Claims.First(c => c.Type == "Username").Value!;

            message.Sender = sender;

            var friendsList = usersession?.Claims.First(c => c.Type == "Friends").Value!;
            if (friendsList.Contains(target))
            {
                await _syncService.AddToDeleteMessage($"dB/", target, message);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
