using System;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Maui.Controls;
using Tuzagym.Services;

namespace Tuzagym.Views
{
    public partial class RecuperarContrasenaPage : ContentPage
    {
        private readonly HttpClient _httpClient;
        private bool _procesando;

        public RecuperarContrasenaPage()
        {
            InitializeComponent();

            // La recuperación usa la misma URL que el login.
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(ApiService.ObtenerBaseUrl()),
                Timeout = TimeSpan.FromSeconds(60)
            };
        }

        private void CambiarCargando(bool cargando)
        {
            _procesando = cargando;
            CargandoIndicator.IsVisible = cargando;
            CargandoIndicator.IsRunning = cargando;
            EnviarCodigoButton.IsEnabled = !cargando;
            CambiarContrasenaButton.IsEnabled = !cargando;
        }

        private async void OnEnviarCodigoClicked(object sender, EventArgs e)
        {
            if (_procesando) return;

            string correo = CorreoEntry.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(correo) || !correo.Contains("@"))
            {
                await DisplayAlertAsync("Correo inválido",
                    "Introduce un correo electrónico válido.", "Aceptar");
                return;
            }

            try
            {
                CambiarCargando(true);
                using var respuesta = await _httpClient.PostAsJsonAsync(
                    "api/auth/solicitar-recuperacion", new { Correo = correo });

                if (respuesta.IsSuccessStatusCode)
                {
                    SeccionRestablecer.IsVisible = true;
                    await DisplayAlertAsync("Revisa tu correo",
                        "Si tu correo está registrado, recibirás un código de recuperación.",
                        "Aceptar");
                }
                else
                {
                    await DisplayAlertAsync("No se pudo procesar",
                        $"La API respondió HTTP {(int)respuesta.StatusCode}. Intenta nuevamente.",
                        "Aceptar");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error de recuperación: {ex}");
                await DisplayAlertAsync("Error de conexión",
                    $"No se pudo conectar con la API de TuzaGym: {ex.Message}", "Aceptar");
            }
            finally { CambiarCargando(false); }
        }

        private async void OnCambiarContrasenaClicked(object sender, EventArgs e)
        {
            if (_procesando) return;

            string correo = CorreoEntry.Text?.Trim() ?? "";
            string codigo = CodigoEntry.Text?.Trim() ?? "";
            string nueva = NuevaContrasenaEntry.Text ?? "";
            string confirmar = ConfirmarContrasenaEntry.Text ?? "";

            if (string.IsNullOrWhiteSpace(correo))
            {
                await DisplayAlertAsync("Correo requerido", "Introduce tu correo electrónico.", "Aceptar");
                return;
            }
            if (codigo.Length != 6 || !long.TryParse(codigo, out _))
            {
                await DisplayAlertAsync("Código inválido", "Introduce el código de 6 dígitos.", "Aceptar");
                return;
            }
            if (nueva.Length < 8)
            {
                await DisplayAlertAsync("Contraseña corta", "La contraseña debe tener al menos 8 caracteres.", "Aceptar");
                return;
            }
            if (nueva != confirmar)
            {
                await DisplayAlertAsync("Las contraseñas no coinciden",
                    "Revisa la confirmación de tu contraseña.", "Aceptar");
                return;
            }

            try
            {
                CambiarCargando(true);
                using var respuesta = await _httpClient.PostAsJsonAsync(
                    "api/auth/restablecer-contrasena",
                    new { Correo = correo, Codigo = codigo, NuevaContrasena = nueva });

                if (respuesta.IsSuccessStatusCode)
                {
                    CodigoEntry.Text = "";
                    NuevaContrasenaEntry.Text = "";
                    ConfirmarContrasenaEntry.Text = "";
                    await DisplayAlertAsync("Contraseña actualizada",
                        "Tu contraseña se cambió correctamente. Ahora puedes iniciar sesión.",
                        "Aceptar");
                    await Navigation.PopModalAsync();
                }
                else
                {
                    await DisplayAlertAsync("No se pudo cambiar",
                        $"La API respondió HTTP {(int)respuesta.StatusCode}. Revisa el código o solicita otro.",
                        "Aceptar");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al restablecer contraseña: {ex}");
                await DisplayAlertAsync("Error de conexión",
                    $"No se pudo conectar con la API de TuzaGym: {ex.Message}", "Aceptar");
            }
            finally { CambiarCargando(false); }
        }

        private async void OnVolverClicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }
}
