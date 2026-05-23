using ChatApp.External;
using ChatApp.Utilities;

namespace ChatApp;

public partial class DeleteAccount : ContentPage
{
    private const string deleteAccountEndpoint = "api/Auth/DeleteAccount";
    private const string notRequired = "notrequired";

    public DeleteAccount()
    {
        InitializeComponent();
        Shell.SetBackgroundColor(this, Color.FromArgb("#000000"));
    }

    private async void OnDeleteAccountClicked(object sender, EventArgs e)
    {
        bool isConfirmed = await DisplayAlert(
            "Confirm Delete",
            "Are you sure you want to delete your account?",
            "Yes",
            "No"
        );

        if (isConfirmed)
        {
            var firebaseService = new FirebaseService(new HttpClient());
            await firebaseService.PostDataAsync(notRequired, deleteAccountEndpoint);

            LocalDbExtensions.RemovePreferences("_user");
            LocalDbExtensions.RemovePreferences("_friendsCounter");
            LocalDbExtensions.RemoveSecureString("_jwttoken");
            LocalDbExtensions.RemoveSecureString("_publicKey");
            LocalDbExtensions.RemoveSecureString("_privateKey");
            await Navigation.PushAsync(new Login(), false);
        }
        else
        {
            Console.WriteLine("Account deletion canceled.");
        }
    }
}
