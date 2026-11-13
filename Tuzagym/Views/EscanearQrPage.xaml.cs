
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace Tuzagym.Views
{
    public partial class EscanearQrPage : ContentPage
    {
        private bool _procesando;

        private readonly Func<string, Task<string>> _registrarQr;

        private readonly CameraBarcodeReaderView _lectorQr;

        public EscanearQrPage(
            Func<string, Task<string>> registrarQr)
        {
            InitializeComponent();

            _registrarQr = registrarQr
                ?? throw new ArgumentNullException(
                    nameof(registrarQr));

            _lectorQr = new CameraBarcodeReaderView
            {
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
                IsDetecting = false,

                Options = new BarcodeReaderOptions
                {
                    Formats = BarcodeFormats.TwoDimensional,
                    AutoRotate = true,
                    Multiple = false
                }
            };

            _lectorQr.BarcodesDetected +=
                LectorQr_BarcodesDetected;

            var contenedor = this.FindByName<Grid>("ContenedorCamara");

            if (contenedor == null)
            {
                throw new InvalidOperationException(
                    "No se encontró ContenedorCamara en EscanearQrPage.xaml.");
            }

            contenedor.Children.Add(_lectorQr);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            var permiso = await Permissions.RequestAsync<
                Permissions.Camera>();

            if (permiso != PermissionStatus.Granted)
            {
                _lectorQr.IsDetecting = false;

                await DisplayAlert(
                    "Permiso necesario",
                    "Debes permitir el acceso a la cámara.",
                    "Aceptar");

                if (Navigation.NavigationStack.Count > 1)
                {
                    await Navigation.PopAsync();
                }

                return;
            }

            _lectorQr.IsDetecting = true;
        }

        protected override void OnDisappearing()
        {
            _lectorQr.IsDetecting = false;
            base.OnDisappearing();
        }

        private void LectorQr_BarcodesDetected(
            object sender,
            BarcodeDetectionEventArgs e)
        {
            var codigo = e.Results
                .FirstOrDefault()?.Value;

            if (string.IsNullOrWhiteSpace(codigo))
                return;

            MainThread.BeginInvokeOnMainThread(
                async () => await ProcesarQrAsync(codigo));
        }

        private async Task ProcesarQrAsync(string codigo)
        {
            if (_procesando)
                return;

            _procesando = true;
            _lectorQr.IsDetecting = false;
            EstadoLabel.Text = "Validando acceso...";

            try
            {
                string mensaje = await _registrarQr(codigo);

                EstadoLabel.Text = mensaje;

                await DisplayAlert(
                    "Resultado de asistencia",
                    mensaje,
                    "Aceptar");
            }
            catch (Exception ex)
            {
                EstadoLabel.Text =
                    "No se pudo validar el acceso.";

                await DisplayAlert(
                    "Error",
                    ex.Message,
                    "Aceptar");
            }
            finally
            {
                _procesando = false;

                if (Navigation.NavigationStack.Contains(this))
                {
                    _lectorQr.IsDetecting = true;
                }
            }
        }

        private async void Volver_Clicked(
            object sender,
            EventArgs e)
        {
            _lectorQr.IsDetecting = false;

            if (Navigation.NavigationStack.Count > 1)
            {
                await Navigation.PopAsync();
            }
        }
    }
}
