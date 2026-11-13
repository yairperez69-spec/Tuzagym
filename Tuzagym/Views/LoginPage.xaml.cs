
using System;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using Tuzagym.Services;
using Tuzagym.ViewModels;

namespace Tuzagym.Views
{
    public partial class LoginPage : ContentPage
    {
        private bool _abriendoRecuperacion = false;

        public LoginPage()
        {
            InitializeComponent();

            // Obtener la instancia compartida de ApiService
            var apiService = IPlatformApplication.Current?
                .Services.GetRequiredService<ApiService>();

            if (apiService == null)
            {
                throw new InvalidOperationException(
                    "No se pudo obtener ApiService.");
            }

            // Crear el ViewModel una sola vez
            BindingContext = new LoginViewModel(apiService);
        }

        // Abrir recuperación de contraseña
        // únicamente al tocar el enlace.
        private async void OnRecuperarContrasenaTapped(
            object sender,
            TappedEventArgs e)
        {
            if (_abriendoRecuperacion)
                return;

            _abriendoRecuperacion = true;

            try
            {
                await Navigation.PushModalAsync(
                    new RecuperarContrasenaPage());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Error al abrir recuperación: {ex}");

                await DisplayAlertAsync(
                    "Error",
                    "No se pudo abrir la recuperación de contraseña.",
                    "Aceptar");
            }
            finally
            {
                _abriendoRecuperacion = false;
            }
        }
    }
}
