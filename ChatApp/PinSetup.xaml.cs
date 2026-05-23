using Android.Content;
using ChatApp.Database;
using ChatApp.Utilities;
using Microsoft.EntityFrameworkCore;
#if ANDROID
using ChatApp.Platforms.Android.Services;
using CommunityToolkit.Maui.Alerts;
#endif
namespace ChatApp;

public partial class PinSetup : ContentPage
{
    private AppDbContext _context;

    private void PinInput_Completed(object sender, EventArgs e)
    {
        var PINcode = LocalDbExtensions.RetrieveSecureString("_pinCode");
        var des = LocalDbExtensions.RetrieveSecureString("_desCode");
        string pin = PinInput.Text;

        if (pin == PINcode)
        {
            PinInput.IsVisible = false;
            t1.IsVisible = true;
            PinToggle.IsToggled = true;
        }
    }

    public PinSetup()
    {
        _context = App.ServiceProvider!.GetRequiredService<AppDbContext>();
        InitializeComponent();
        Shell.SetBackgroundColor(this, Color.FromArgb("#000000"));

        var PINcode = LocalDbExtensions.RetrieveSecureString("_pinCode");
        if (!string.IsNullOrEmpty(PINcode))
        {
            PinInput.IsVisible = true;
            t1.IsVisible = false;
        }
    }

    private async void PinToggle_Toggled(object sender, ToggledEventArgs e)
    {
        bool isOn = e.Value;
        if (!isOn)
        {
            LocalDbExtensions.RemoveSecureString("_pinCode");
            LocalDbExtensions.RemoveSecureString("_desCode");
            await _context.Messages.ExecuteUpdateAsync(s => s.SetProperty(p => p.IsHidden, false));
        }
        PinSection.IsVisible = isOn;
        DestructionSection.IsVisible = isOn;
    }

    private async void DestructionEntry_Completed(object sender, EventArgs e)
    {
        var _pincode = PinEntry.Text;
        string destructionPin = DestructionEntry.Text;
        LocalDbExtensions.SaveSecureString("_pinCode", _pincode);
        LocalDbExtensions.SaveSecureString("_desCode", destructionPin);
#if ANDROID
        await Task.Run(async () =>
        {
            await Snackbar
                .Make("PIN and Destruction code saved!", duration: TimeSpan.FromMilliseconds(900))
                .Show();
        });
#endif
    }
}
