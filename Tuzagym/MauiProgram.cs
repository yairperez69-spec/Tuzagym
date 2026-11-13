
using Microsoft.Extensions.Logging;
using ZXing.Net.Maui.Controls;
using Tuzagym.Services;

namespace Tuzagym;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseBarcodeReader()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont(
                    "OpenSans-Regular.ttf",
                    "OpenSansRegular");

                fonts.AddFont(
                    "OpenSans-Semibold.ttf",
                    "OpenSansSemibold");
            });

     
        builder.Services.AddSingleton<ApiService>();

        builder.Services.AddSingleton<IApiService>(
            sp => sp.GetRequiredService<ApiService>());

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
