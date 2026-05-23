using System;
using System.Net.Http.Headers;
using ChatApp.Models;
using ChatApp.Utilities;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json.Linq;
#if ANDROID
using Android.Webkit;
using ChatApp.Platforms.Android;
#endif



namespace ChatApp;

public partial class Calling : ContentPage
{
    private const string key = "_user";
    private const string jwttoken = "_jwttoken";
    private string user;
    private string jwt;
    private string _friendname;

    public Calling(string? friendUsername)
    {
        InitializeComponent();
        _friendname = friendUsername!;
        user = LocalDbExtensions.RetrievePreferences(key);
        jwt = LocalDbExtensions.RetrieveSecureString(jwttoken);
    }

    protected override bool OnBackButtonPressed()
    {
        return true;
    }

    private void Init(string? friendUsername, string user)
    {
        if (!string.IsNullOrEmpty(user))
        {
            string url = string.IsNullOrEmpty(friendUsername)
                ? Utilities.Configuration.FrontendURL
                    + "call/?key="
                    + user
                    + "&random="
                    + Guid.NewGuid().ToString()
                : Utilities.Configuration.FrontendURL
                    + "call/?key="
                    + user
                    + "&calleekey="
                    + friendUsername
                    + "&random="
                    + Guid.NewGuid().ToString();

            webView.Source = url;

            if (!string.IsNullOrEmpty(friendUsername))
            {
                if (OperatingSystem.IsAndroidVersionAtLeast(33))
                {
                    MainActivity.SetAudioToEarpiece();
                }

                webView.Navigated += async (s, e) =>
                {
                    await webView.EvaluateJavaScriptAsync("startAudio()");
                };
            }

            SetupJavaScriptBridge();

            webView.Reload();
            #if ANDROID
            webView.HandlerChanged += (s, e) =>
            {
                if (webView.Handler?.PlatformView is Android.Webkit.WebView nativeWebView)
                {
                    nativeWebView.Settings.JavaScriptEnabled = true;
                    nativeWebView.Settings.MediaPlaybackRequiresUserGesture = false;
                    nativeWebView.Settings.DomStorageEnabled = true;
                    nativeWebView.Settings.JavaScriptCanOpenWindowsAutomatically = true;
                    CookieManager.Instance!.SetAcceptThirdPartyCookies(nativeWebView, true);
                    CookieManager.Instance.SetCookie(
                        Utilities.Configuration.FrontendURLNoSlash,
                        "sessionId=" + jwt
                    );
                    CookieManager.Instance.Flush();
                    nativeWebView.SetWebChromeClient(new CustomWebChromeClient());
                }
            };
            #endif
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Init(_friendname, user);

        #if ANDROID
        MainActivity.StartProximity();
        #endif
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        WeakReferenceMessenger.Default.Send(new OnChangeInCallVariableFalse());
        #if ANDROID
        MainActivity.StopProximity();
        #endif
        await Shell.Current.Navigation.PopToRootAsync();
    }

    private void SetupJavaScriptBridge()
    {
        #if ANDROID
        webView.HandlerChanged += (s, e) =>
        {
            if (webView.Handler?.PlatformView is Android.Webkit.WebView nativeWebView)
            {
                // Create a JavaScript interface
                nativeWebView.AddJavascriptInterface(new JSBridge(this), "jsBridge");
            }
        };
        #endif
    }

    private class JSBridge : Java.Lang.Object
    {
        private readonly Calling _calling;

        private bool IsUsingEarpiece = true;

        public JSBridge(Calling calling)
        {
            _calling = calling;
        }

        [JavascriptInterface]
        [Java.Interop.Export("setAudioToEarpiece")]
        public void SetAudioToEarpiece()
        {
            MainActivity.SetAudioToEarpiece();
        }

        [JavascriptInterface]
        [Java.Interop.Export("setAudioToSpeaker")]
        public void SetAudioToSpeaker()
        {
            MainActivity.SetAudioToSpeaker();
        }

        [JavascriptInterface]
        [Java.Interop.Export("closeCall")]
        public void CloseCall()
        {
            Shell.Current.Navigation.PopToRootAsync();
        }

        [JavascriptInterface]
        [Java.Interop.Export("onSwitchAudioClicked")]
        public void onSwitchAudioClicked()
        {
            if (IsUsingEarpiece)
            {
            #if ANDROID
                MainActivity.SetAudioToSpeaker();
                IsUsingEarpiece = false;
            #endif
            }
            else
            {
            #if ANDROID
                MainActivity.SetAudioToEarpiece();
                IsUsingEarpiece = false;
            #endif
                IsUsingEarpiece = true;
            }
        }
    }
}
