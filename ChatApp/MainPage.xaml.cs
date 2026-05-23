using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Android.Content;
using ChatApp.Database;
using ChatApp.External;
using ChatApp.Models;
using ChatApp.Utilities;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using static System.Net.Mime.MediaTypeNames;
using static Android.Icu.Text.CaseMap;
#if ANDROID
using CommunityToolkit.Maui.Alerts;
#endif
namespace ChatApp
{
    public partial class MainPage : ContentPage
    {
        private const string refreshTokenEndpoint = "api/Auth/RefreshToken2";
        private const string notOK = "not ok";
        private const string updateDeviceEndpoint = "api/Auth/UpdateDeviceId";
        private const string userFriendsEndpoint = "api/Auth/GetUserFriendsAsync";
        private const string notRequired = "not required";
        private const string getUnameswithProfileEndpoint = "api/Auth/GetUserNamesWithProfileAsync";
        string _user = string.Empty;
        private AppDbContext _context;
        private readonly FirebaseService _firebaseService;
        private readonly HttpClient _httpClient = new HttpClient();
        public ObservableCollection<ProfileUser> UsersFromFire { get; set; } =
            new ObservableCollection<ProfileUser>();
        private Dictionary<string, UserCopy> UsernameProfileUser =
            new Dictionary<string, UserCopy>();
        private string _friendsCounter = string.Empty;
        private Dictionary<string, string> _cachedDeviceIds = new Dictionary<string, string>();
        private Dictionary<string, string?> _cachedProfilePics = new Dictionary<string, string?>();

        private async void InitializeLogin(string username)
        {
            try
            {
                var _jwt = LocalDbExtensions.RetrieveSecureString("_jwttoken");
                var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    "Bearer",
                    _jwt
                );
                var firebaseService = new FirebaseService(client);
                var bearer = await firebaseService.PostDataAsync(username, refreshTokenEndpoint);

                if (bearer != notOK)
                {
                    LocalDbExtensions.SaveSecureString("_jwttoken", bearer);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Initialization error: {ex.Message}");
            }
        }

        private async void PinInput_Completed(object sender, EventArgs e)
        {
            var PINcode = LocalDbExtensions.RetrieveSecureString("_pinCode");
            var des = LocalDbExtensions.RetrieveSecureString("_desCode");
            string pin = PinInput.Text;

            if (pin == PINcode)
            {
                PinInput.IsVisible = false;

                WhenAllMethods();
                _ = RefreshFromServerAsync();
            }
            else if (pin == des)
            {
                LocalDbExtensions.RemovePreferences("_notifiTitle");
                LocalDbExtensions.RemovePreferences("_notifiBody");
                PinInput.IsVisible = false;
                await _context.Messages.ExecuteUpdateAsync(s =>
                    s.SetProperty(p => p.IsHidden, true)
                );
                WhenAllMethods();
                _ = RefreshFromServerAsync();
            }
        }

        public MainPage()
        {
            Shell.SetBackgroundColor(this, Color.FromArgb("#000000"));
            _user = LocalDbExtensions.RetrievePreferences("_user");
            var PINcode = LocalDbExtensions.RetrieveSecureString("_pinCode");

            if (string.IsNullOrEmpty(_user))
            {
                Navigation.PushAsync(new Login(), false);
            }
            else
            {
                _context = App.ServiceProvider!.GetRequiredService<AppDbContext>();
                _firebaseService = new FirebaseService(_httpClient);
                _friendsCounter = LocalDbExtensions.RetrievePreferences("_friendsCounter");
                InitializeComponent();
                UsersFromFire = new ObservableCollection<ProfileUser>();
                BindingContext = this;

                if (string.IsNullOrEmpty(PINcode))
                {
                    WhenAllMethods();
                    _ = RefreshFromServerAsync();
                }
                else
                {
                    PinInput.IsVisible = true;
                    LoadingLbl.Text = "";
                }
                WeakReferenceMessenger.Default.Register<PullMainListEvent>(
                    this,
                    async (r, message) =>
                    {
                        await InitializeList();
                    }
                );
            }
        }

        private async void WhenAllMethods()
        {
            var notifiTitle = LocalDbExtensions.RetrievePreferences("_notifiTitle");
            var notifiBody = LocalDbExtensions.RetrievePreferences("_notifiBody");
            var _cachedDevices = LocalDbExtensions.RetrievePreferences("_cachedDevicesId");

            if (!string.IsNullOrEmpty(_cachedDevices))
            {
                var _cachedProfiles = LocalDbExtensions.RetrievePreferences("_cachedProfilePics");

                var deserializedProfilePics = new Dictionary<string, string?>();
                if (!string.IsNullOrEmpty(_cachedProfiles))
                {
                    deserializedProfilePics = JsonSerializer.Deserialize<
                        Dictionary<string, string?>
                    >(_cachedProfiles);
                }
                var deserializedDevices = JsonSerializer.Deserialize<Dictionary<string, string>>(
                    _cachedDevices
                );

                if (deserializedDevices != null)
                {
                    foreach (var kvp in deserializedDevices)
                    {
                        var newKey = kvp.Key;

                        UsernameProfileUser[newKey] = new UserCopy { DeviceId = kvp.Value };

                        _cachedDeviceIds[newKey] = kvp.Value;

                        string? base64sm = null;
                        if (
                            deserializedProfilePics != null
                            && deserializedProfilePics.ContainsKey(newKey)
                        )
                        {
                            base64sm = deserializedProfilePics[newKey];
                        }
                        var user = new ProfileUser
                        {
                            LatestMessage =
                                kvp.Key == notifiTitle
                                    ? new Message
                                    {
                                        Id = Guid.NewGuid().ToString(),
                                        Sender = _user,
                                        SentDate = DateTime.Now.ToString(),
                                        IsClient = false,
                                        Text =
                                            (
                                                notifiBody.Length > 20
                                                    ? notifiBody.Substring(0, 20)
                                                    : notifiBody
                                            )
                                            + ": "
                                            + DateTime.Now.ToString("HH:mm"),
                                        IsRead = false,
                                    }
                                    : null,
                            Username = newKey,
                            Base64Image = base64sm!,
                        };

                        if (kvp.Key == notifiTitle)
                            UsersFromFire.Insert(0, user);
                        else
                            UsersFromFire.Add(user);
                    }
                }
            }
            else
            {
                var _deviceId = LocalDbExtensions.RetrieveSecureString("_deviceId");
                var updateDevideId = await _firebaseService.PostDataAsync(
                    _deviceId,
                    updateDeviceEndpoint
                );
                var userfriends = await _firebaseService.PostDataAsyncReturnListOfUsers(
                    notRequired,
                    userFriendsEndpoint
                );
                if (userfriends != null)
                {
                    if (string.IsNullOrEmpty(_friendsCounter))
                    {
                        LocalDbExtensions.SaveJsonPreferences(
                            "_friendsCounter",
                            userfriends.Count().ToString()
                        );
                    }
                    else
                    {
                        var convertToInt = Convert.ToInt32(_friendsCounter);
                        if (userfriends.Count() != convertToInt)
                        {
                            InitializeLogin(_user);
                            LocalDbExtensions.SaveJsonPreferences(
                                "_friendsCounter",
                                userfriends.Count().ToString()
                            );
                        }
                    }

                    var query = string.Join(",", userfriends.Select(a => a.Username));
                    var POSTresult = await _firebaseService.GetUserNamesWithProfileAsync(
                        query,
                        getUnameswithProfileEndpoint
                    );

                    if (POSTresult != null)
                    {
                        foreach (var kvp in POSTresult)
                        {
                            var newKey = kvp.Key;
                            var imageSource = new ProfileUser
                            {
                                Base64Image = kvp.Value.base64sm!,
                            }.ImageSource;

                            UsernameProfileUser[newKey] = new UserCopy
                            {
                                ImageSource = imageSource,
                                DeviceId = kvp.Value.DeviceId,
                            };

                            _cachedDeviceIds[newKey] = kvp.Value.DeviceId;
                            _cachedProfilePics[newKey] = kvp.Value.base64sm;
                        }

                        foreach (var user in userfriends)
                        {
                            if (POSTresult.TryGetValue(user.Username, out UserCopy? userCopy))
                            {
                                UsersFromFire.Add(
                                    new ProfileUser
                                    {
                                        LatestMessage = _context
                                            .Messages.Where(s =>
                                                s.SelectedReceiverId == user.Username
                                                || s.SelectedReceiverId == _user
                                            )
                                            .OrderByDescending(c => c.Timestamp)
                                            .Select(a => new Message
                                            {
                                                Text =
                                                    (
                                                        a.Content.Length > 20
                                                            ? a.Content.Substring(0, 20)
                                                            : a.Content
                                                    )
                                                    + ": "
                                                    + a.Timestamp.ToString("HH:mm"),
                                                Id = a.MessageId,
                                                Sender = a.SenderId,
                                                SentDate = a.Timestamp.ToString(),
                                                IsRead = a.IsRead,
                                                ShowWebView = false,
                                                IsClient = a.IsClient,
                                            })
                                            .FirstOrDefault(),
                                        Username = user.Username,
                                        Base64Image = userCopy.base64!,
                                    }
                                );
                            }
                        }

                        OnlineUsersListView.ItemsSource = UsersFromFire;
                        string jCachedDeviceIds = JsonSerializer.Serialize(_cachedDeviceIds);
                        LocalDbExtensions.SaveJsonPreferences("_cachedDevicesId", jCachedDeviceIds);
                        string jCachedProfilePics = JsonSerializer.Serialize(_cachedProfilePics);
                        const int SevenHundredKB = 700 * 1024;
                        int sizeInBytes = Encoding.UTF8.GetByteCount(jCachedProfilePics);
                        if (sizeInBytes < SevenHundredKB)
                        {
                            LocalDbExtensions.SaveJsonPreferences(
                                "_cachedProfilePics",
                                jCachedProfilePics
                            );
                        }
                    }
                }
            }
        }

        private Grid _lastSelectedGrid;
        private Frame _lastSelectedFrame;

        private async void OnStoryButtonClicked(object sender, EventArgs e)
        {
            var tappedEventArgs = e as TappedEventArgs;
            if (tappedEventArgs != null)
            {
                var tappedUsername = tappedEventArgs.Parameter as string;
                if (tappedUsername != null)
                {
                    var popup = new ImageViewer(tappedUsername);
                    var r = await this.ShowPopupAsync(popup);
                }
            }
        }

        private async void OnChatButtonClicked(object sender, EventArgs e)
        {
            if (sender is not Frame frame)
                return;

            if (_lastSelectedFrame != null)
                _lastSelectedFrame.BackgroundColor = Colors.Transparent;

            // Highlight the clicked frame
            frame.BackgroundColor = Colors.DarkSlateGray;
            _lastSelectedFrame = frame;

            // Your existing code to handle navigation
            var tappedEventArgs = e as TappedEventArgs;
            if (tappedEventArgs != null)
            {
                var tappedUsername = tappedEventArgs.Parameter as string;
                if (tappedUsername != null)
                {
                    var tt = new StartChat(
                        tappedUsername,
                        UsernameProfileUser[tappedUsername].DeviceId
                    );
                    await Navigation.PushAsync(tt, false);
                }
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (string.IsNullOrEmpty(_user))
            {
                Navigation.PushAsync(new Login(), false);
            }
        }

        private async Task WhenAllAfter()
        {
            var _cachedDevices = LocalDbExtensions.RetrievePreferences("_cachedDevicesId");
            if (!string.IsNullOrEmpty(_cachedDevices))
            {
                var userfriends = await _firebaseService.PostDataAsyncReturnListOfUsers(
                    notRequired,
                    userFriendsEndpoint
                );
                if (userfriends != null)
                {
                    if (string.IsNullOrEmpty(_friendsCounter))
                    {
                        LocalDbExtensions.SaveJsonPreferences(
                            "_friendsCounter",
                            userfriends.Count().ToString()
                        );
                    }
                    else
                    {
                        var convertToInt = Convert.ToInt32(_friendsCounter);
                        if (userfriends.Count() != convertToInt)
                        {
                            InitializeLogin(_user);
                            LocalDbExtensions.SaveJsonPreferences(
                                "_friendsCounter",
                                userfriends.Count().ToString()
                            );
                        }
                    }

                    var query = string.Join(",", userfriends.Select(a => a.Username));
                    var POSTresult = await _firebaseService.GetUserNamesWithProfileAsync(
                        query,
                        getUnameswithProfileEndpoint
                    );

                    if (POSTresult != null)
                    {
                        foreach (var kvp in POSTresult)
                        {
                            var newKey = kvp.Key;
                            var imageSource = new ProfileUser
                            {
                                Base64Image = kvp.Value.base64sm!,
                            }.ImageSource;

                            UsernameProfileUser[newKey] = new UserCopy
                            {
                                ImageSource = imageSource,
                                DeviceId = kvp.Value.DeviceId,
                            };

                            _cachedDeviceIds[newKey] = kvp.Value.DeviceId;
                            _cachedProfilePics[newKey] = kvp.Value.base64sm;
                        }

                        string jCachedDeviceIds = JsonSerializer.Serialize(_cachedDeviceIds);
                        LocalDbExtensions.SaveJsonPreferences("_cachedDevicesId", jCachedDeviceIds);

                        string jCachedProfilePics = JsonSerializer.Serialize(_cachedProfilePics);

                        const int SevenHundredKB = 700 * 1024;
                        int sizeInBytes = Encoding.UTF8.GetByteCount(jCachedProfilePics);
                        if (sizeInBytes < SevenHundredKB)
                        {
                            LocalDbExtensions.SaveJsonPreferences(
                                "_cachedProfilePics",
                                jCachedProfilePics
                            );
                        }
                    }
                }
            }
        }

        private async Task InitializeList()
        {
            UsersFromFire = new ObservableCollection<ProfileUser>();
            var userfriends = await _firebaseService.PostDataAsyncReturnListOfUsers(
                notRequired,
                userFriendsEndpoint
            );
            if (userfriends != null)
            {
                var query = string.Join(",", userfriends.Select(a => a.Username));
                var POSTresult = await _firebaseService.GetUserNamesWithProfileAsync(
                    query,
                    getUnameswithProfileEndpoint
                );
                if (POSTresult != null)
                {
                    foreach (var kvp in POSTresult)
                    {
                        var newKey = kvp.Key;
                        var imageSource = new ProfileUser
                        {
                            Base64Image = kvp.Value.base64sm!,
                        }.ImageSource;

                        UsernameProfileUser[newKey] = new UserCopy
                        {
                            ImageSource = imageSource,
                            DeviceId = kvp.Value.DeviceId,
                        };
                    }

                    foreach (var user in userfriends)
                    {
                        if (POSTresult.TryGetValue(user.Username, out UserCopy? userCopy))
                        {
                            UsersFromFire.Add(
                                new ProfileUser
                                {
                                    LatestMessage = _context
                                        .Messages.Where(s =>
                                            (s.ReceiverId == user.Username && s.SenderId == _user)
                                            || (
                                                s.ReceiverId == _user && s.SenderId == user.Username
                                            )
                                        )
                                        .OrderByDescending(c => c.Timestamp)
                                        .Select(a => new Message
                                        {
                                            Text =
                                                (
                                                    a.Content.Length > 20
                                                        ? a.Content.Substring(0, 20)
                                                        : a.Content
                                                )
                                                + ": "
                                                + a.Timestamp.ToString("HH:mm"),
                                            Id = a.MessageId,
                                            Sender = a.SenderId,
                                            SentDate = a.Timestamp.ToString(),
                                            IsRead = a.IsRead,
                                            ShowWebView = false,
                                            IsClient = a.IsClient,
                                        })
                                        .FirstOrDefault(),
                                    Username = user.Username,
                                    Base64Image = userCopy.base64!,
                                }
                            );
                        }
                    }

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        var t = UsersFromFire.OrderByDescending(a => a.LatestMessage?.SentDate);
                        OnlineUsersListView.ItemsSource = t;
                    });
                }
            }
        }

        private async Task RefreshFromServerAsync()
        {
            try
            {
                await WhenAllAfter();
            }
            catch (Exception ex)
            {
                // Log or handle exception to avoid crash on background thread
                Console.WriteLine($"Refresh failed: {ex}");
            }
        }
    }
}
