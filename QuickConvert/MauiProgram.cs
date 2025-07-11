using Microsoft.Extensions.Logging;
using QuickConvert.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Compatibility.Hosting;
using CommunityToolkit.Maui;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Controls.Hosting;

namespace QuickConvert
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCompatibility()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIcons");
                });

            // Register services
            builder.Services.AddSingleton<DatabaseService>();
            
            // Register pages
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<BookmarkPage>();
            builder.Services.AddTransient<WalletPage>();
            builder.Services.AddTransient<ChartPage>();
#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}

