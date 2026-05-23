using System.Runtime.CompilerServices;
using ChatApp.Encryption;
using ChatApp.External;
using ChatApp.Utilities;
using CommunityToolkit.Maui.Views;
using static Android.Icu.Text.CaseMap;

namespace ChatApp;

public partial class ImageViewer : Popup
{
    private const string storyEndpoint = "api/Sync/GetStory";
    private string _deviceId;
    private FirebaseService firebaseService = new FirebaseService(new HttpClient());

    private void OnCloseButtonClicked(object sender, EventArgs e)
    {
        Close();
    }

    private void StartAnimation()
    {
        ProgressFill.Animate(
            "Story",
            v =>
            {
                ProgressFill.WidthRequest = ProgressContainer.Width * v;
            },
            length: 5000
        );
    }

    public ImageViewer(string uname)
    {
        InitializeComponent();
        _deviceId = LocalDbExtensions.RetrieveSecureString("_deviceId");
        load(uname);
        StartAnimation();
    }

    public async void load(string uname)
    {
        var t = await firebaseService.GetStoryAsync(uname, storyEndpoint);
        if (t != null && t.Text != "noText" && t.Sender == uname)
        {
            byte[] bytes = Convert.FromBase64String(t.Text);
            var encryptedBytes = EncryptionRSA.DecryptNew(bytes, _deviceId, _deviceId);

            if (encryptedBytes != null)
            {
                DynamicImage.Source = ImageSource.FromStream(() =>
                    new MemoryStream(encryptedBytes)
                );
                ChatExtensions.StoryUserName = "";
            }
        }
        else
        {
            byte[] bytes = Convert.FromBase64String(ChatExtensions.DefaultBase64Image);
            DynamicImage.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
        }
    }
}
