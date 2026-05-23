using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ChatApp.Encryption;
using ChatApp.External;
using ChatApp.Models;
using ChatApp.Utilities;
#if ANDROID
using CommunityToolkit.Maui.Alerts;
#endif
namespace ChatApp;

public partial class CheckUserName : ContentPage
{
    private const string key = "_deviceId";
    private const string cannotRegister = "You cannot register if you do not accept our terms!";
    private const string selectValidName =
        "Please select a valid username or a valid password that is at least 6 chars";
    private const string selectValidNameOnly = "Please select a valid username";
    private const string checkUserEndpoint = "api/Auth/CheckUser";
    private const string loginEndpoint = "api/Auth/Login";
    private const string sendFriendRequestEndpoint = "api/Auth/SendFriendRequestOwn";
    private const string firstLogin = "_firstLogin";
    private const string userNameTaken = "UserName is taken, please choose another one";
    private const string notOK = "not ok";
    private const string couldNotLogin = "Could not login!";
    private const string getDeviceId = "api/Auth/GetStaleDeviceIdsAsync";
    private const string sendNotificationEndpoint = "api/Auth/SendNotification";
    private const string pleaseRegister =
        "Could not login! Please register if you do not have an account!";
    private readonly HttpClient _httpClient;
    private string _deviceId;
    public bool IsRegistering { get; set; }

    public CheckUserName(bool isRegistering)
    {
        InitializeComponent();
        IsRegistering = isRegistering;
        BindingContext = this;
        _httpClient = new HttpClient();
        _deviceId = LocalDbExtensions.RetrieveSecureString(key);
        NavigationPage.SetHasBackButton(this, false);
        NavigationPage.SetHasNavigationBar(this, false);
        Shell.SetNavBarIsVisible(this, false);
    }

    private async void OnEntryCompleted(object sender, EventArgs e)
    {
#if ANDROID
        await Task.Run(async () =>
        {
            await Snackbar.Make("Please wait..", duration: TimeSpan.FromMilliseconds(400)).Show();
        });
#endif

        if (!IsRegistering)
        {
            await OnLoginButtonClickedAsync();
        }
        else
        {
            await OnRegisterButtonClickedAsync();
        }
    }

    private async Task OnRegisterButtonClickedAsync()
    {
        var firebaseService = new FirebaseService(new HttpClient());
        string userInput = UserInput.Text.Trim();
        string password = PasswordEntry.Text;

        bool isChecked = checkbox1.IsChecked;
        bool isCheckedAge = checkboxAge.IsChecked;

        if (!isChecked || !isCheckedAge)
        {
            ErrorLabel.IsVisible = false;
            SuccessLabel.IsVisible = false;
            ErrorLabel.Text = cannotRegister;
            ErrorLabel.IsVisible = true;
            return;
        }

        if (
            string.IsNullOrEmpty(userInput)
            || userInput.Length < 6
            || string.IsNullOrEmpty(password)
            || password.Length < 6
        )
        {
            ErrorLabel.IsVisible = false;
            SuccessLabel.IsVisible = false;
            ErrorLabel.Text = selectValidName;
            ErrorLabel.IsVisible = true;
            return;
        }

        bool isValid = Regex.IsMatch(userInput, @"^[a-zA-Z0-9]+$");

        if (!isValid)
        {
            ErrorLabel.IsVisible = false;
            SuccessLabel.IsVisible = false;
            ErrorLabel.Text = selectValidNameOnly;
            ErrorLabel.IsVisible = true;
            return;
        }

        if (!string.IsNullOrEmpty(userInput))
        {
            var result = await firebaseService.PostDataAsync(userInput, checkUserEndpoint);

            if (string.IsNullOrEmpty(result))
            {
                await LoginDataAsync(userInput, password, _deviceId, loginEndpoint);
                await firebaseService.PostDataAsync(userInput, sendFriendRequestEndpoint);
                LocalDbExtensions.SaveJsonPreferences(firstLogin, "True");
                await Shell.Current.Navigation.PopToRootAsync();
            }
            else
            {
                ErrorLabel.IsVisible = false;
                SuccessLabel.IsVisible = false;
                ErrorLabel.Text = userNameTaken;
                ErrorLabel.IsVisible = true;
            }
        }
    }

    private async Task OnLoginButtonClickedAsync()
    {
        var firebaseService = new FirebaseService(new HttpClient());
        string userInput = UserInput.Text.Trim();
        string password = PasswordEntry.Text;
        if (!string.IsNullOrEmpty(userInput))
        {
            var result = await firebaseService.PostDataAsync(userInput, checkUserEndpoint);
            if (!string.IsNullOrEmpty(result))
            {
                var bearer = await LoginDataAsync(userInput, password, _deviceId, loginEndpoint);
                if (bearer == notOK)
                {
                    ErrorLabel.IsVisible = false;
                    SuccessLabel.IsVisible = false;
                    ErrorLabel.Text = couldNotLogin;
                    ErrorLabel.IsVisible = true;
                }
                else
                {
                    var (publicKeyString, privateKeyString) = EncryptionRSA.GeneratePublicKey();
                    LocalDbExtensions.SaveJsonPreferences("_user", userInput);
                    LocalDbExtensions.SaveSecureString("_jwttoken", bearer);
                    LocalDbExtensions.SaveSecureString("_publicKey", publicKeyString);
                    LocalDbExtensions.SaveSecureString("_privateKey", privateKeyString);
                    SuccessLabel.Text = $"Name '{userInput}' has been saved successfully";
                    SuccessLabel.IsVisible = true;
                    var first = LocalDbExtensions.RetrievePreferences(firstLogin);

                    if (first == "True")
                    {
                        var firebaseService2 = new FirebaseService(new HttpClient());
                        var t1 = await firebaseService2.GetInvitesAsync(userInput, getDeviceId);
                        var cutoffDate = DateTime.Now.AddDays(-2);

                        if (t1 != null)
                        {
                            var recentIds = t1
                                .Values.OfType<Invite>()
                                .Where(d => d.SentAt >= cutoffDate && d.DeviceId != _deviceId)
                                .Select(d => d.DeviceId)
                                .ToList();
                            foreach (var item in recentIds)
                            {
                                if (item != null)
                                {
                                    var POSTresult = await firebaseService2.PostDataTwoAsync(
                                        item,
                                        EncryptionRSA.EncryptString(
                                            "Add #" + userInput + "# as a friend",
                                            item,
                                            item
                                        ),
                                        sendNotificationEndpoint
                                    );
                                }
                            }
                        }
                        LocalDbExtensions.SaveJsonPreferences(firstLogin, "False");
                    }
                    Application.Current.MainPage = new AppShell { };
                }
            }
            else
            {
                ErrorLabel.IsVisible = false;
                SuccessLabel.IsVisible = false;
                ErrorLabel.Text = pleaseRegister;
                ErrorLabel.IsVisible = true;
            }
        }
    }

    public async Task<string> LoginDataAsync(
        string input,
        string password,
        string deviceId,
        string url2
    )
    {
        string url = Utilities.Configuration.BackendURL + url2;
        var data = new
        {
            Username = input,
            DeviceId = deviceId,
            HashedPW = password,
        };
        string jsonData = JsonSerializer.Serialize(data);
        var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
        try
        {
            HttpResponseMessage response = await _httpClient.PostAsync(url, content);
            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Success: {responseBody}");
                return responseBody;
            }
            else
            {
                return notOK;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            return notOK;
        }
    }
}
