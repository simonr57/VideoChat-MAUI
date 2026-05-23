using System.Collections.ObjectModel;
using ChatApp.External;
using ChatApp.Models;

namespace ChatApp;

public partial class ContactListVy : ContentPage
{
    private const string updateInvite = "api/Auth/UpdateInviteSentAtAsync";
    private const string notRequired = "notrequired";
    private const string shareVia = "Share via";
    private const string playStoreUri = "https://play.google.com/store/apps/details?id=com.chitgab";

    public ObservableCollection<ContactModel> OnlineUsers { get; set; } =
        new ObservableCollection<ContactModel>();

    public ContactListVy()
    {
        InitializeComponent();
        Shell.SetBackgroundColor(this, Color.FromArgb("#000000"));
        BindingContext = this;
    }

    private async void OnShareClicked(object sender, EventArgs e)
    {
        var firebaseService = new FirebaseService(new HttpClient());
        var POSTresult = await firebaseService.PostDataAsync(notRequired, updateInvite);

        await Share.Default.RequestAsync(
            new ShareTextRequest { Uri = playStoreUri, Title = shareVia }
        );
    }

    private async Task<bool> CheckAndRequestPermissions()
    {
        var statusContact = await Permissions.RequestAsync<Permissions.ContactsRead>();
        if (statusContact != PermissionStatus.Granted)
        {
            statusContact = await Permissions.RequestAsync<Permissions.ContactsRead>();
        }
        return statusContact == PermissionStatus.Granted;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CheckAndRequestPermissions();
        MainThread.BeginInvokeOnMainThread(() =>
        {
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;
        });

#if ANDROID
        Task.Run(async () =>
        {
            OnlineUsers.Clear();
            await LoadContactsAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
            });
        });
#endif
    }

#if ANDROID
    async Task<List<Contact>> GetAllContactsAsync()
    {
        return (await Contacts.Default.GetAllAsync()).ToList();
    }

    public async Task LoadContactsAsync()
    {
        var list = await GetAllContactsAsync();
        foreach (var c in list)
            OnlineUsers.Add(new ContactModel { DisplayName = c.DisplayName });
    }
#endif
}
