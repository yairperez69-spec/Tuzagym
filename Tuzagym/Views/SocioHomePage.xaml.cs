
using System;
using Microsoft.Maui.Controls;
using Tuzagym.Models;
using Tuzagym.Services;
using Tuzagym.ViewModels;

namespace Tuzagym.Views
{
    public partial class SocioHomePage : ContentPage
    {
        private readonly SocioHomeViewModel? _viewModel;

        public SocioHomePage()
        {
            InitializeComponent();
        }

        public SocioHomePage(LoginResult usuario) : this()
        {
            if (usuario == null ||
                !usuario.Exito ||
                usuario.IdUsuario <= 0)
            {
                throw new ArgumentException(
                    "La sesión del socio no es válida.",
                    nameof(usuario));
            }

            _viewModel = new SocioHomeViewModel(
                usuario,
                new ApiService());

            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (_viewModel == null)
                return;

            try
            {
                await _viewModel.CargarRutinaAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Error",
                    "No se pudo cargar la rutina: " + ex.Message,
                    "Aceptar");
            }
        }

        // =====================================
        // NAVEGACIÓN: INICIO
        // =====================================

        private async void Inicio_Tapped(
            object sender,
            TappedEventArgs e)
        {
            await ContenidoSocio.ScrollToAsync(
                0,
                0,
                true);
        }

        // =====================================
        // NAVEGACIÓN: MI CÓDIGO QR
        // =====================================

        private async void MiQr_Tapped(
            object sender,
            TappedEventArgs e)
        {
            await ContenidoSocio.ScrollToAsync(
                SeccionQr,
                ScrollToPosition.Start,
                true);
        }

        // =====================================
        // NAVEGACIÓN: RUTINAS
        // =====================================

        private async void Rutina_Tapped(
            object sender,
            TappedEventArgs e)
        {
            if (_viewModel != null)
            {
                try
                {
                    await _viewModel.CargarRutinaAsync();
                }
                catch (Exception ex)
                {
                    await DisplayAlert(
                        "Error",
                        "No se pudo actualizar la rutina: " +
                        ex.Message,
                        "Aceptar");
                }
            }

            await ContenidoSocio.ScrollToAsync(
                SeccionRutina,
                ScrollToPosition.Start,
                true);
        }
    }
}
