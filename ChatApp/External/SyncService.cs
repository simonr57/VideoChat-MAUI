using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ChatApp.Database;
using ChatApp.Encryption;
using ChatApp.Models;
using ChatApp.Utilities;
using Microsoft.EntityFrameworkCore;
#if ANDROID
using Android.Media;
#endif
#if ANDROID
using CommunityToolkit.Maui.Alerts;
#endif
namespace ChatApp.External
{
    public class SyncService
    {
        private readonly string jwt = "_jwttoken";
        private readonly string user = "_user";
        private readonly string deviceId = "_deviceId";
        private readonly string notRequired = "notrequired";
        private readonly string encrypted = "Encrypted";
        private readonly string syncMessagesEndpoint = "api/Sync/SyncMessages";
        private readonly string deletePendingMessagesEndpoint = "api/Auth/DeleteAllPendingMessages";
        private string _user = string.Empty;
        private string _jwt = string.Empty;

        private AppDbContext _context;

        public SyncService()
        {
            _context = App.ServiceProvider!.GetRequiredService<AppDbContext>();
            _jwt = LocalDbExtensions.RetrieveSecureString(jwt);
            _user = LocalDbExtensions.RetrievePreferences(user);
        }

        private string DecryptFirebase(string text)
        {
            try
            {
                var _deviceId = LocalDbExtensions.RetrieveSecureString(deviceId);
                var decryptedMessage = EncryptionRSA.DecryptString(text, _deviceId, _deviceId);
                return decryptedMessage;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return encrypted;
            }
        }

        public static readonly SemaphoreSlim _syncSemaphore = new SemaphoreSlim(1, 1);

        public async Task SyncMessages()
        {
            await _syncSemaphore.WaitAsync();
            try
            {
                var firebaseService = new FirebaseService(new HttpClient());
                var syncClientService = new SyncClientService(new HttpClient());

                var dict = await syncClientService.SyncFireData(syncMessagesEndpoint);
                if (dict != null && dict.Count > 0)
                {
                    var existingMessageIds = await _context
                        .Messages.Select(m => m.MessageId)
                        .ToListAsync();

                    var newEntities = dict.Where(kv => !existingMessageIds.Contains(kv.Value.Id))
                        .Select(kv =>
                        {
                            return new dBMessage
                            {
                                Content = DecryptFirebase(kv.Value.Text),
                                MessageId = kv.Value.Id,
                                IsRead = false,
                                IsView = kv.Value.ShowWebView,
                                IsClient = false,
                                Timestamp = kv.Value.SentDate,
                                SenderId = kv.Value.Sender,
                                ReceiverId = _user,
                                IsDelivered = true,
                                SelectedReceiverId = kv.Value.Sender,
                                IsHidden = false,
                            };
                        })
                        .ToList();

                    var entitiesAwaited = newEntities;

                    if (entitiesAwaited.Any())
                    {
                        try
                        {
                            await _context.Messages.AddRangeAsync(entitiesAwaited);
                            if (await _context.SaveChangesAsync() > 0)
                            {
                                await firebaseService.PostDataAsync(
                                    notRequired,
                                    deletePendingMessagesEndpoint
                                );
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            await firebaseService.PostDataAsync(
                                notRequired,
                                deletePendingMessagesEndpoint
                            );
                        }
                    }
                    else
                    {
                        await firebaseService.PostDataAsync(
                            notRequired,
                            deletePendingMessagesEndpoint
                        );
                    }
                }
            }
            finally
            {
                _syncSemaphore.Release();
            }
        }
    }
}
