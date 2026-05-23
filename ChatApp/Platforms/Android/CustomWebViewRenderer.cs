using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Content;
using Android.Webkit;
using Microsoft.Maui.Controls.Compatibility.Platform.Android;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using WebView = Android.Webkit.WebView;

namespace ChatApp.Platforms.Android;

public class CustomWebViewHandler : WebViewHandler
{
    protected override WebView CreatePlatformView()
    {
        var webView = base.CreatePlatformView();
        webView.Settings.JavaScriptEnabled = true;
        webView.Settings.MediaPlaybackRequiresUserGesture = false;
        webView.Settings.DomStorageEnabled = true;
        webView.Settings.AllowFileAccess = true;
        webView.Settings.DatabaseEnabled = true;
        webView.Settings.SetSupportZoom(true);
        webView.Settings.LoadWithOverviewMode = true;
        webView.Settings.UseWideViewPort = true;
        webView.Settings.MixedContentMode = MixedContentHandling.AlwaysAllow;
        webView.Settings.JavaScriptCanOpenWindowsAutomatically = true;
        webView.SetWebChromeClient(new CustomWebChromeClient());
        webView.SetWebViewClient(new CustomWebViewClient());
        webView.LoadUrl(
            Utilities.Configuration.FrontendURL
                + "call/?key=caller&random="
                + Guid.NewGuid().ToString()
        );
        return webView;
    }
}

public class CustomWebChromeClient : WebChromeClient
{
    public override void OnPermissionRequest(PermissionRequest request)
    {
        request.Grant(request.GetResources());
    }
}

public class CustomWebViewClient : WebViewClient
{
    public override bool ShouldOverrideUrlLoading(WebView view, IWebResourceRequest request)
    {
        return false;
    }
}
