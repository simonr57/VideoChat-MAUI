using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Web;
using ChatApp.Utilities;
using CommunityToolkit.Maui.Views;
#if ANDROID
using ChatApp.Platforms.Android.Services;
using CommunityToolkit.Maui.Alerts;
using Android.Media;
#endif
namespace ChatApp;

public partial class CharsPopup : Popup
{
    private const string jwt = "_jwttoken";
    private const string scheme = "Bearer";
    private readonly HttpClient _httpClient;

    public CharsPopup()
    {
        InitializeComponent();
        SetFullScreenSize();
        BindingContext = this;

        _httpClient = new HttpClient();
        var _jwt = LocalDbExtensions.RetrieveSecureString(jwt);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            scheme,
            _jwt
        );

        store.Navigating += async (s, e) =>
        {
            try
            {
                if (e.Url.StartsWith("js-message://base64ready"))
                {
                    e.Cancel = true;

                    try
                    {
                        var jsResult = await store.EvaluateJavaScriptAsync("window.base64Data");
                        var base64Data = jsResult.ToString(); // Convert result to string
                        await store.EvaluateJavaScriptAsync("delete window.base64Data;");
                        SaveGif(base64Data);
                    }
                    catch (Exception ex)
                    {
                        // Handle errors
                    }
                }
                else if (e.Url.StartsWith("js-message://"))
                {
                    e.Cancel = true; // prevent actually navigating
                    var uri = new Uri(e.Url);
                    var qs = HttpUtility.ParseQueryString(uri.Query);
                    var payload = qs["value"];
                    string pattern = @"^[a-zA-Z0-9_:]+$";

                    if (payload != null)
                    {
                        bool isValid = Regex.IsMatch(payload, pattern);
                        if (isValid)
                        {
                            ChatExtensions.charId = payload;
                            Close();
                        }
                    }
                }
            }
            catch (Exception exx)
            {
                Console.WriteLine(exx.StackTrace);
            }
        };
    }

    private async void SaveGif(string base64Data)
    {
        try
        {
            if (string.IsNullOrEmpty(base64Data))
                return;

            var base64String = base64Data.Split(',')[1];
            var bytes = Convert.FromBase64String(base64String);

            var fileName = $"dance_{DateTime.Now:yyyyMMdd_HHmmss}.gif";

            string? path = "";
            #if ANDROID
            path = Android
                .OS.Environment.GetExternalStoragePublicDirectory(
                    Android.OS.Environment.DirectoryPictures
                )
                ?.AbsolutePath;

            #elif WINDOWS
            path = "";
            #endif

            var folderPath = Path.Combine(path!, "ChitGab");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var filePath = Path.Combine(folderPath, fileName);
            try
            {
                using (
                    var stream = new FileStream(
                        filePath,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None
                    )
                )
                {
                    await stream.WriteAsync(bytes, 0, bytes.Length);
                }

            #if ANDROID
                MediaScannerConnection.ScanFile(
                    Android.App.Application.Context,
                    new string[] { filePath },
                    null,
                    null
                );
            #endif

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await App.Current.MainPage.DisplayAlert(
                        "Success",
                        $"GIF saved to {fileName}",
                        "OK"
                    );
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving GIF: {ex.Message}");
        }
    }

    private void SetFullScreenSize()
    {
        var displayInfo = DeviceDisplay.Current.MainDisplayInfo;
        var density = displayInfo.Density;
        var width = displayInfo.Width / density;
        var height = displayInfo.Height / density;
        Size = new Size(width, height);
    }
}
