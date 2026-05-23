using ChatApp.Utilities;

namespace ChatApp;

public partial class Logout : ContentPage
{
    public Logout()
    {
        InitializeComponent();
        LocalDbExtensions.RemovePreferences("_user");
        LocalDbExtensions.RemovePreferences("_friendsCounter");
        LocalDbExtensions.RemoveSecureString("_jwttoken");
        LocalDbExtensions.RemoveSecureString("_publicKey");
        LocalDbExtensions.RemoveSecureString("_privateKey");
        Navigation.PushAsync(new Login(), false);
    }
}
