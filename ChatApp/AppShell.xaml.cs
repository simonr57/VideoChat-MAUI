using System.Text.Json;
using ChatApp.External;
using ChatApp.Models;
using ChatApp.Utilities;

namespace ChatApp
{
    public partial class AppShell : Shell
    {
        private const string jwt = "_jwttoken";
        private const string user = "_user";
        private const string notOK = "not ok";
        private const string getProfileImage = "api/Auth/GetProfilePictureSMAsync";
        private const string getProfileImageBase = "api/Auth/GetProfilePictureAsync";
        private const string updateProfileImage = "api/Auth/UpdateProfilePictureAsync";
        string _user = string.Empty;

        public AppShell()
        {
            InitializeComponent();
            SetFlyoutVisibilityBasedOnAuth();
            Routing.RegisterRoute("Home", typeof(MainPage));
            Task.Run(LoadUserDataAsync);
            Task.Run(LoadClientImage);
        }

        private async Task LoadClientImage()
        {
            var firebaseService = new FirebaseService(new HttpClient());
            var clientSMProfile = firebaseService.PostDataAsync(_user, getProfileImage);
            await clientSMProfile!;
        }

        private async Task LoadUserDataAsync()
        {
            if (!string.IsNullOrEmpty(LocalDbExtensions.RetrieveSecureString(jwt)))
            {
                _user = LocalDbExtensions.RetrievePreferences(user);
                SetUserNameLabel();
                await SetProfileImageIfExistAsync();
            }
        }

        private void SetFlyoutVisibilityBasedOnAuth()
        {
            var token = LocalDbExtensions.RetrieveSecureString(jwt);

            if (string.IsNullOrEmpty(token))
            {
                this.FlyoutBehavior = FlyoutBehavior.Disabled;
            }
            else
            {
                this.FlyoutBehavior = FlyoutBehavior.Flyout;
            }
        }

        private void SetUserNameLabel()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                LblUsername.Text = "#" + _user;
                LblUsername.FontSize = 18;
            });
        }

        private async Task SetProfileImageIfExistAsync()
        {
            var firebaseService = new FirebaseService(new HttpClient());
            var POSTresult = await firebaseService.PostDataAsync(
                "doesnotneed",
                getProfileImageBase
            );

            if (POSTresult != null && POSTresult != notOK)
            {
                byte[] imageBytes = Convert.FromBase64String(POSTresult);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    imgProfile.Source = ImageSource.FromStream(() => new MemoryStream(imageBytes));
                });
            }
        }

        private async Task<byte[]> GetImageBytesFromStream(Stream stream)
        {
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }

        private async void OnProfileTapped(object sender, EventArgs e)
        {
            try
            {
                var result = await MediaPicker.PickPhotoAsync(
                    new MediaPickerOptions { Title = "Select a photo" }
                );

                if (result != null)
                {
                    var stream = await result.OpenReadAsync();
                    var barray = await GetImageBytesFromStream(stream);

                    barray = ImageHelper.FixImageOrientation(barray);

                    var resized1 = ChatExtensions.CreateThumbnail(barray, 500, 500);
                    imgProfile.Source = ImageSource.FromStream(() => new MemoryStream(resized1));

                    string base64String1 = Convert.ToBase64String(resized1);

                    var resized2 = ChatExtensions.CreateThumbnail(barray, 50, 50);
                    string base64String2 = Convert.ToBase64String(resized2);

                    var firebaseService = new FirebaseService(new HttpClient());
                    await firebaseService.PostDataTwoAsync(
                        base64String1,
                        base64String2,
                        updateProfileImage
                    );
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }
    }
}
