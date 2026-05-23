using System.Net.Http.Headers;
using System.Runtime.Versioning;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Service.Notification;
using Android.Widget;
using AndroidX.Core.App;
using ChatApp.External;
using ChatApp.Models;
using ChatApp.Utilities;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.Controls;

namespace ChatApp
{
    [SupportedOSPlatform("android33.0")]
    [Activity(
        Theme = "@style/Maui.SplashTheme",
        MainLauncher = true,
        LaunchMode = LaunchMode.SingleTop,
        ConfigurationChanges = ConfigChanges.ScreenSize
            | ConfigChanges.Orientation
            | ConfigChanges.UiMode
            | ConfigChanges.ScreenLayout
            | ConfigChanges.SmallestScreenSize
            | ConfigChanges.Density,
        ScreenOrientation = ScreenOrientation.Portrait
    )]
    public class MainActivity : MauiAppCompatActivity
    {
        private const string jwt = "_jwttoken";
        private const string scheme = "Bearer";
        private const string refreshToken = "api/Auth/RefreshToken";
        private const string notOK = "not ok";
        private const string _body = "body";
        private const string AddFriend = "as a friend";
        private const string freindRequest = "Friendrequest";
        private const string navigateTo = "navigateTo";
        private const string calling = "Calling";
        private const string updateDevice = "api/Auth/UpdateDeviceId";
        private const string deviceId = "_deviceId";
        private const string user = "_user";
        private string _user = string.Empty;

        public static void SetAudioToEarpiece()
        {
            var audioManager = (AudioManager)
                Platform.CurrentActivity.GetSystemService(Context.AudioService);

            audioManager.Mode = Mode.InCommunication;
            audioManager.SpeakerphoneOn = false;
            audioManager.StopBluetoothSco();
            audioManager.BluetoothScoOn = false;

            if (OperatingSystem.IsAndroidVersionAtLeast(31))
            {
                var devices = audioManager.GetDevices(GetDevicesTargets.Outputs);
                var earpiece = devices.FirstOrDefault(d =>
                    d.Type == AudioDeviceType.BuiltinEarpiece
                );
                if (earpiece != null)
                {
                    audioManager.SetCommunicationDevice(earpiece);
                }
            }
        }

        public static void SetAudioToSpeaker()
        {
            var audioManager = (AudioManager)
                Platform.CurrentActivity.GetSystemService(Context.AudioService);
            audioManager.Mode = Mode.Normal;
            audioManager.SpeakerphoneOn = true;
        }

        private static PowerManager.WakeLock _wakeLock;

        public static void StartProximity()
        {
            var powerManager = (PowerManager)
                Platform.CurrentActivity.GetSystemService(Context.PowerService);

            if (_wakeLock == null)
            {
                _wakeLock = powerManager.NewWakeLock(
                    WakeLockFlags.ProximityScreenOff,
                    "ChatApp:ProximityWakeLock"
                );
            }

            if (!_wakeLock.IsHeld)
            {
                _wakeLock.Acquire();
            }
        }

        public static void StopProximity()
        {
            if (_wakeLock != null && _wakeLock.IsHeld)
            {
                _wakeLock.Release();
            }
        }

        private async void InitializeLogin(string username)
        {
            try
            {
                var _jwt = LocalDbExtensions.RetrieveSecureString(jwt);
                var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    scheme,
                    _jwt
                );
                var firebaseService = new FirebaseService(client);
                var bearer = await firebaseService.PostDataAsync(username, refreshToken);

                if (bearer == notOK) { }
                else
                {
                    LocalDbExtensions.SaveSecureString(jwt, bearer);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Initialization error: {ex.Message}");
            }
        }

        private void InitSync()
        {
            _ = Task.Run(async () =>
            {
                var syncService = App.ServiceProvider!.GetRequiredService<SyncService>();
                await syncService.SyncMessages();
            });
        }

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            HandleIntent(intent, false);
        }

        private void HandleIntent(Intent intent, bool isFromCreate)
        {
            string body = intent.GetStringExtra(_body)!;

            if (!string.IsNullOrEmpty(body) && body.Contains(AddFriend))
            {
                var getUname = body!.Split("#");
                WeakReferenceMessenger.Default.Send(new OnFriendSuggestAdd(getUname[1]));
            }

            if (!string.IsNullOrEmpty(body) && body.Contains(freindRequest))
            {
                var getUname = body!.Split("#");
                WeakReferenceMessenger.Default.Send(new OnFriendrequestAdd(getUname[1]));
            }

            if (intent?.Extras != null && intent.HasExtra(navigateTo))
            {
                string destination = intent.GetStringExtra(navigateTo);
                if (destination == calling)
                {
                    WeakReferenceMessenger.Default.Send(new OnChangeInCallVariable());

                    ConnectionHubService connectionHubService = new ConnectionHubService();

                    WeakReferenceMessenger.Default.Send(new StartCallEvent());
                }
            }
            else
            {
                InitSync();
            }
        }

        protected override async void OnDestroy()
        {
            base.OnDestroy();
            WeakReferenceMessenger.Default.Send(new OnUpdateDatabase());
            await Task.Delay(TimeSpan.FromSeconds(30));
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Window.SetStatusBarColor(Android.Graphics.Color.ParseColor("#000000"));

            _ = Task.Run(async () =>
            {
                var _jwt = LocalDbExtensions.RetrieveSecureString(jwt);
                if (!string.IsNullOrEmpty(_jwt))
                {
                    var client = new HttpClient();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                        scheme,
                        _jwt
                    );
                    var firebaseService = new FirebaseService(client);
                    var _deviceId = LocalDbExtensions.RetrieveSecureString(deviceId);
                    await firebaseService.PostDataAsync(_deviceId, updateDevice);
                }
                try
                {
                    _user = LocalDbExtensions.RetrievePreferences(user);
                    if (!string.IsNullOrEmpty(_user))
                    {
                        InitializeLogin(_user);
                    }
                    HandleIntent(Intent, true);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            });
        }
    }
}
