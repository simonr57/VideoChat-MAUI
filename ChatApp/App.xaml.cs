using System.Diagnostics;
using ChatApp.Database;
using ChatApp.Encryption;
using ChatApp.External;
using ChatApp.Models;
using ChatApp.Utilities;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Hosting;
#if ANDROID
using ChatApp.Platforms.Android.Services;
using CommunityToolkit.Maui.Alerts;
#endif

namespace ChatApp
{
    public partial class App : Application
    {
        private const string sendRequest = "api/Auth/SendFriendRequest";
        private const string getUID = "api/Auth/GetUserDeviceIdByUsernameAsync";
        private const string sendNotification = "api/Auth/SendNotification";

        public static IServiceProvider? ServiceProvider { get; private set; }

        public App(IServiceProvider services)
        {
            InitializeComponent();

            Application.Current!.UserAppTheme = AppTheme.Dark;

            MainPage = new AppShell();

            WeakReferenceMessenger.Default.Register<StartCallEvent>(
                this,
                (r, message) =>
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        var cll = new Calling(null);
                        await Shell.Current.Navigation.PushAsync(cll, false);
                    });
                }
            );

            WeakReferenceMessenger.Default.Register<OnFriendSuggestAdd>(
                this,
                (r, message) =>
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        var firebaseService = new FirebaseService(new HttpClient());
                        var t = await firebaseService.PostDataAsync(
                            message.FriendName,
                            sendRequest
                        );
                        var result = await firebaseService.PostDataAsync(
                            message.FriendName,
                            getUID
                        );
                        var POSTresult = await firebaseService.PostDataTwoAsync(
                            result!,
                            EncryptionRSA.EncryptString(
                                "Friendrequest ## Tap to add",
                                result!,
                                result!
                            ),
                            sendNotification
                        );

#if ANDROID
                        alerts("Request sent to #" + message.FriendName);
#endif
                    });
                }
            );

            WeakReferenceMessenger.Default.Register<OnFriendrequestAdd>(
                this,
                (r, message) =>
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        var t = new FriendsRequests();
                        await Shell.Current.Navigation.PushAsync(t, false);
                    });
                }
            );

            _ = Task.Run(() =>
            {
                ServiceProvider = services;
                var backgroundService = services.GetService<IHostedService>();
                backgroundService?.StartAsync(CancellationToken.None);

                using (var scope = services.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    if (dbContext.Database.EnsureCreated())
                    {
                        Console.WriteLine("Database was created successfully.");
                    }
                }
            });
        }

#if ANDROID
        private async void alerts(string val)
        {
            await Task.Run(async () =>
            {
                await Snackbar.Make(val, duration: TimeSpan.FromMilliseconds(900)).Show();
            });
        }
#endif

        protected override void OnSleep()
        {
            base.OnSleep();

            if (ChatExtensions.screenIsOff == false)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Shell.Current.Navigation.PopToRootAsync();
                });
            }
        }
    }
}
