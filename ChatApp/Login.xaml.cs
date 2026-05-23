using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows.Input;
using ChatApp.External;
using ChatApp.Models;
using ChatApp.Utilities;
using Microsoft.AspNetCore.SignalR.Client;

namespace ChatApp;

public partial class Login : ContentPage
{
    public Login()
    {
        InitializeComponent();
        NavigationPage.SetHasBackButton(this, false);
        NavigationPage.SetHasNavigationBar(this, false);
        Shell.SetNavBarIsVisible(this, false);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CheckAndRequestPermissions();
    }

    private void OnSelectClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is string param)
        {
            var page = new CheckUserName(param == "Register" ? true : false);
            Navigation.PushAsync(page, false);
        }
    }

    private async Task<bool> CheckAndRequestPermissions()
    {
        var postStatus = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
        if (postStatus != PermissionStatus.Granted)
        {
            postStatus = await Permissions.RequestAsync<Permissions.PostNotifications>();
        }
        var microphoneStatus = await Permissions.CheckStatusAsync<Permissions.Microphone>();
        if (microphoneStatus != PermissionStatus.Granted)
        {
            microphoneStatus = await Permissions.RequestAsync<Permissions.Microphone>();
            microphoneStatus = await Permissions.RequestAsync<Permissions.Speech>();
        }
        var statusPhone = await Permissions.CheckStatusAsync<Permissions.Phone>();
        if (statusPhone != PermissionStatus.Granted)
        {
            statusPhone = await Permissions.RequestAsync<Permissions.Phone>();
        }
        var statusStorage = await Permissions.RequestAsync<Permissions.StorageWrite>();
        if (statusStorage != PermissionStatus.Granted)
        {
            statusStorage = await Permissions.RequestAsync<Permissions.StorageWrite>();
        }
        var statusCamera = await Permissions.RequestAsync<Permissions.Camera>();
        if (statusCamera != PermissionStatus.Granted)
        {
            statusCamera = await Permissions.RequestAsync<Permissions.Camera>();
        }
        return microphoneStatus == PermissionStatus.Granted
            && postStatus == PermissionStatus.Granted
            && statusPhone == PermissionStatus.Granted
            && statusStorage == PermissionStatus.Granted
            && statusCamera == PermissionStatus.Granted;
    }
}
