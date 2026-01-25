#if ANDROID
using ChatApp.Platforms.Android;
#endif
using ChatApp.Database;
using ChatApp.External;
using CommunityToolkit.Maui;
using Microsoft.EntityFrameworkCore;
using ChatApp.Workers;
using Microsoft.Extensions.Logging;

namespace ChatApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite($"Filename={Path.Combine(FileSystem.AppDataDirectory, "app.db")}"));
            builder.Services.AddSingleton<FirebaseService>();
            builder.Services.AddSingleton<SyncService>();
            builder
                .UseMauiApp<App>()
                .ConfigureMauiHandlers(handlers =>
                {
                    #if ANDROID
                    handlers.AddHandler(typeof(WebView), typeof(CustomWebViewHandler));
                    #endif
                })
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                    {
                        fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                        fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    });

            builder.Services.AddHostedService<MyBackgroundService>();
            #if DEBUG
            builder.Logging.AddDebug();
            #endif
            return builder.Build();
        }
    }
}
