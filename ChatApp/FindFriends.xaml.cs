using System.Collections.Generic;
using System.Text.Json;
using ChatApp.Encryption;
using ChatApp.External;
using ChatApp.Models;
using ChatApp.Utilities;
using static System.Net.Mime.MediaTypeNames;

namespace ChatApp
{
    public partial class FindFriends : ContentPage
    {
        private const string Key = "_user";
        private const string SendFriendRequestEndpoint = "api/Auth/SendFriendRequest";
        private const string notOK = "not ok";
        private const string sendNotificationEndpoint = "api/Auth/SendNotification";
        private const string allUserSearchEndpoint = "api/Auth/AllUsersBySearch";
        private FirebaseService firebaseService = new FirebaseService(new HttpClient());
        string _user = string.Empty;

        public FindFriends()
        {
            _user = LocalDbExtensions.RetrievePreferences(Key);
            InitializeComponent();
            Shell.SetBackgroundColor(this, Color.FromArgb("#000000"));
        }

        private async void OnAddFriendButtonClicked(object sender, EventArgs e)
        {
            var button = sender as Button;

            if (button != null)
            {
                var selectedUser = button.CommandParameter as ProfileUser;

                if (selectedUser != null)
                {
                    button.IsEnabled = false;
                    var endpoint = await firebaseService.PostDataAsync(
                        selectedUser.Username!,
                        SendFriendRequestEndpoint
                    );
                    if (endpoint != notOK)
                    {
                        var POSTresult = await firebaseService.PostDataTwoAsync(
                            selectedUser.DeviceId!,
                            EncryptionRSA.EncryptString(
                                "Friendrequest #" + _user + "# Tap to add",
                                selectedUser.DeviceId!,
                                selectedUser.DeviceId!
                            ),
                            sendNotificationEndpoint
                        );
                    }
                }
            }
        }

        private async void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(e.NewTextValue))
            {
                OnlineUsersListView.ItemsSource = null;
                return;
            }
            if (!string.IsNullOrEmpty(e.NewTextValue) && e.NewTextValue.Length >= 3)
            {
                var dictionary = await firebaseService.PostDataAsyncReturnDict(
                    e.NewTextValue,
                    allUserSearchEndpoint
                );

                if (dictionary == null)
                {
                    OnlineUsersListView.ItemsSource = null;
                    return;
                }

                if (!string.IsNullOrEmpty(_user))
                {
                    dictionary.Remove(_user);
                }
                var userList = dictionary
                    .Select(kvp => new ProfileUser
                    {
                        Username = kvp.Key,
                        Base64Image =
                            kvp.Value == null
                                ? ChatExtensions.DefaultBase64Image
                                : kvp.Value.base64sm!,
                        DeviceId = kvp.Value!.DeviceId,
                    })
                    .ToList();

                OnlineUsersListView.ItemsSource = userList;
            }
        }
    }
}
