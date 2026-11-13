using Tuzagym.Views;

namespace Tuzagym
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Carga la estructura de navegación principal
            MainPage = new AppShell();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(MainPage);
        }
    }
}