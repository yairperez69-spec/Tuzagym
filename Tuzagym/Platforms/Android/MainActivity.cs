
using Android.App;
using Android.Content.PM;
using Android.OS;

namespace Tuzagym
{
    [Activity(
        Theme = "@style/Maui.SplashTheme",
        MainLauncher = true,
        ConfigurationChanges =
            ConfigChanges.ScreenSize |
            ConfigChanges.Orientation |
            ConfigChanges.UiMode |
            ConfigChanges.ScreenLayout |
            ConfigChanges.SmallestScreenSize |
            ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(
            Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Mantener la configuración nativa
            // de las barras del sistema.
            // Los colores y áreas seguras se
            // configurarán según la versión Android.
        }
    }
}
