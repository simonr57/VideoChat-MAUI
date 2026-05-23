using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using AndroidX.Core.App;
using ChatApp.Encryption;
using ChatApp.Utilities;
using Firebase.Messaging;
using static Android.Icu.Text.CaseMap;
using Uri = Android.Net.Uri;

namespace ChatApp.Platforms.Android.Services
{
    [Service(Exported = true)]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
    public class MyFirebaseMessagingService : FirebaseMessagingService
    {
        const string TAG = "MyFirebaseMsgService";
        const string CHANNEL_ID = "FCM_CHANNEL";
        const string CHANNEL_ID_CALL = "FCM_CHANNEL_CALL";
        const string CHANNEL_NAME = "FCM Message";
        const string CHANNEL_NAME_CALL = "FCM Call";
        const string TITLE = "title";
        const string BODY = "body";
        const string CALL = "Call";
        const string NAVIGATETO = "navigateTo";
        const string CALLING = "Calling";
        const string ENCRYPTED = "Encrypted";
        const string DEVICEID = "_deviceId";
        const string SHAREDIMAGE = "Shared Image";
        const string NOTIFITITLE = "_notifiTitle";
        const string NOTIFIBODY = "_notifiBody";

        public override void OnNewToken(string token)
        {
            base.OnNewToken(token);
            Log.Debug(TAG, $"Refreshed token: {token}");
            SendRegistrationToServer(token);
        }

        public override void OnMessageReceived(RemoteMessage message)
        {
            base.OnMessageReceived(message);

            if (
                message.Data.TryGetValue(TITLE, out string title)
                && message.Data.TryGetValue(BODY, out string body)
            )
            {
                if (body == CALL)
                {
                    SendNotification(title, body);
                }
                else
                {
                    SendNotificationMsg(title, body);
                }
            }
        }

        void SendNotification(string title, string messageBody)
        {
            var intent = new Intent(this, typeof(MainActivity));
            intent.PutExtra(NAVIGATETO, CALLING);
            intent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);

            var pendingIntent = PendingIntent.GetActivity(
                this,
                0,
                intent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable
            );

            string channelId = CHANNEL_ID_CALL;
            string channelName = CHANNEL_NAME_CALL;
            var importance = NotificationImportance.High;
            var notificationManager = (NotificationManager)GetSystemService(NotificationService);

            var soundUri = Uri.Parse(
                ContentResolver.SchemeAndroidResource
                    + "://"
                    + PackageName
                    + "/"
                    + Resource.Raw.notific
            );

            var channel = new NotificationChannel(channelId, channelName, importance);
            channel.SetSound(soundUri, null);

            if (notificationManager != null)
            {
                notificationManager.CreateNotificationChannel(channel);
            }

            var notificationBuilder = new NotificationCompat.Builder(this, channelId)
                .SetContentTitle(title)
                .SetContentText(messageBody)
                .SetSmallIcon(Resource.Drawable.ic_call_answer)
                .SetCategory(Notification.CategoryCall)
                .SetContentIntent(pendingIntent)
                .SetAutoCancel(true);

            notificationBuilder.SetSound(soundUri);

            if (notificationManager != null)
            {
                notificationManager.Notify(new Random().Next(), notificationBuilder.Build());
            }

            PowerManager powerManager = (PowerManager)GetSystemService(PowerService);
            if (powerManager != null)
            {
                using (
                    var wakeLock = powerManager.NewWakeLock(
                        WakeLockFlags.ScreenBright | WakeLockFlags.AcquireCausesWakeup,
                        "MyApp::WakeLockTag"
                    )
                )
                {
                    if (wakeLock != null)
                    {
                        wakeLock.Acquire(3000);
                    }
                }
            }
        }

        void SendNotificationMsg(string title, string messageBody)
        {
            messageBody = DecryptFirebase(messageBody);
            if (messageBody == SHAREDIMAGE)
            {
                ChatExtensions.StoryUserName = title;
            }

            LocalDbExtensions.SaveJsonPreferences(NOTIFITITLE, title);
            LocalDbExtensions.SaveJsonPreferences(NOTIFIBODY, messageBody);

            var intent = new Intent(this, typeof(MainActivity));
            intent.AddFlags(ActivityFlags.ClearTop);
            intent.PutExtra(TITLE, title);
            intent.PutExtra(BODY, messageBody);
            var pendingIntent = PendingIntent.GetActivity(
                this,
                0,
                intent,
                PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent
            );

            string channelId = CHANNEL_ID;
            string channelName = CHANNEL_NAME;
            var importance = NotificationImportance.High;
            var notificationManager = (NotificationManager)GetSystemService(NotificationService);

            var channel = new NotificationChannel(channelId, channelName, importance);

            if (notificationManager != null)
            {
                notificationManager.CreateNotificationChannel(channel);
            }

            var notificationBuilder = new NotificationCompat.Builder(this, channelId)
                .SetContentTitle(title)
                .SetContentText(messageBody)
                .SetCategory(Notification.CategoryMessage)
                .SetContentIntent(pendingIntent)
                .SetSmallIcon(Resource.Drawable.msgicon)
                .SetAutoCancel(true);

            if (notificationManager != null)
            {
                notificationManager.Notify(new Random().Next(), notificationBuilder.Build());
            }

            PowerManager powerManager = (PowerManager)GetSystemService(PowerService);
            if (powerManager != null)
            {
                using (
                    var wakeLock = powerManager.NewWakeLock(
                        WakeLockFlags.ScreenBright | WakeLockFlags.AcquireCausesWakeup,
                        "MyApp::WakeLockTag"
                    )
                )
                {
                    if (wakeLock != null)
                    {
                        wakeLock.Acquire(3000);
                    }
                }
            }
        }

        private string DecryptFirebase(string text)
        {
            try
            {
                var _deviceId = LocalDbExtensions.RetrieveSecureString(DEVICEID);
                var decryptedMessage = EncryptionRSA.DecryptString(text, _deviceId, _deviceId);
                return decryptedMessage;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return ENCRYPTED;
            }
        }

        void SendRegistrationToServer(string token)
        {
            LocalDbExtensions.SaveSecureString(DEVICEID, token);
        }
    }
}
