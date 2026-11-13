
using System;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using Tuzagym.Services;
using Tuzagym.ViewModels;

namespace Tuzagym.Views
{
    public partial class RecepcionHomePage : ContentPage
    {
        private readonly RecepcionHomeViewModel _viewModel;

        public RecepcionHomePage()
        {
            InitializeComponent();

            // Obtener la misma instancia de ApiService
            // utilizada durante el inicio de sesión.
            var apiService = IPlatformApplication.Current?
                .Services.GetRequiredService<IApiService>()
                ?? throw new InvalidOperationException(
                    "No se pudo obtener IApiService.");

            _viewModel = new RecepcionHomeViewModel(apiService);

            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                await _viewModel.CargarEstadisticasAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"ERROR RECEPCIÓN: {ex}");
            }
        }
    }
}
