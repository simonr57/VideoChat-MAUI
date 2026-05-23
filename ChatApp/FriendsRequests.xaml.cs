using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using System.Text.Json;
using ChatApp.External;
using ChatApp.Models;
using ChatApp.Utilities;
using CommunityToolkit.Mvvm.Messaging;
#if ANDROID
using CommunityToolkit.Maui.Alerts;
#endif
namespace ChatApp;

public partial class FriendsRequests : ContentPage
{
    private const string listIncommingEndpoint = "api/Auth/ListIncommingRequests";
    private const string notRequired = "not required";
    private const string getUsernameEndpoint = "api/Auth/GetUserNamesWithProfileAsync";
    private const string acceptFriendEndpoint = "api/Auth/AcceptFriendRequest";
    private const string refreshTokenEndpoint = "api/Auth/RefreshToken2";
    private const string notOK = "not ok";
    private const string deleteFriendReqEndpoint = "api/Auth/DeleteFriendRequest";
    private FirebaseService firebaseService = new FirebaseService(new HttpClient());
    public ObservableCollection<ProfileUser> OnlineUsers { get; set; } =
        new ObservableCollection<ProfileUser>();

    public FriendsRequests()
    {
        InitializeComponent();
        Shell.SetBackgroundColor(this, Color.FromArgb("#000000"));
    }

    private async Task InitializeList()
    {
        var incomingRequests = await firebaseService.PostDataAsyncReturnListOfRequests(
            notRequired,
            listIncommingEndpoint
        );
        if (incomingRequests != null)
        {
            var query = string.Join(",", incomingRequests.Select(a => a.SenderUserId));
            var POSTresult = await firebaseService.GetUserNamesWithProfileAsync(
                query,
                getUsernameEndpoint
            );

            if (POSTresult != null)
            {
                foreach (var user in incomingRequests)
                {
                    if (POSTresult.TryGetValue(user.SenderUserId, out UserCopy? userCopy))
                    {
                        OnlineUsers.Add(
                            new ProfileUser
                            {
                                Username = user.SenderUserId,
                                Base64Image = userCopy.base64!,
                            }
                        );
                    }
                }
                OnlineUsersListView.ItemsSource = OnlineUsers;
            }
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        OnlineUsers.Clear();
        await InitializeList();
    }

    private async void OnAddFriendButtonClicked(object sender, EventArgs e)
    {
        var button = sender as Button;

        if (button != null)
        {
            var selectedUser = button.CommandParameter;

            if (selectedUser != null)
            {
                button.IsEnabled = false;
                var endpoint = await firebaseService.PostDataAsync(
                    selectedUser.ToString()!,
                    acceptFriendEndpoint
                );

                if (endpoint != notOK)
                {
                    var _jwt = LocalDbExtensions.RetrieveSecureString("_jwttoken");
                    var client = new HttpClient();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                        "Bearer",
                        _jwt
                    );
                    var firebaseService2 = new FirebaseService(client);
                    var bearer = await firebaseService2.PostDataAsync("", refreshTokenEndpoint);

                    if (bearer == notOK) { }
                    else
                    {
                        LocalDbExtensions.SaveSecureString("_jwttoken", bearer);
                    }
                    WeakReferenceMessenger.Default.Send(new PullMainListEvent());
                }
            }
        }
    }

    private async void OnRemoveFriendButtonClicked(object sender, EventArgs e)
    {
        var button = sender as Button;

        if (button != null)
        {
            var selectedUser = button.CommandParameter;

            if (selectedUser != null)
            {
                button.IsEnabled = false;
                var t = await firebaseService.PostDataAsync(
                    selectedUser.ToString()!,
                    deleteFriendReqEndpoint
                );
            }
        }
    }
}
