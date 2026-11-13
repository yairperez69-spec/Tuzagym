
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tuzagym.Services;
using Tuzagym.Models;

namespace Tuzagym.ViewModels
{
    public class AdminHomeViewModel : INotifyPropertyChanged
    {
        private readonly ApiService _api;

        public AdminHomeViewModel(ApiService api)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
        }

        // VENTAS DEL MES
        private string _ventasMes = "—";

        public string VentasMes
        {
            get => _ventasMes;
            set => SetProperty(ref _ventasMes, value);
        }

        // GANANCIAS DEL MES
        private string _gananciaMes = "—";

        public string GananciaMes
        {
            get => _gananciaMes;
            set => SetProperty(ref _gananciaMes, value);
        }

        // GASTOS DEL MES
        private string _gastosMes = "—";

        public string GastosMes
        {
            get => _gastosMes;
            set => SetProperty(ref _gastosMes, value);
        }

        // SALDO DE CAJA
        private string _saldoCaja = "—";

        public string SaldoCaja
        {
            get => _saldoCaja;
            set => SetProperty(ref _saldoCaja, value);
        }

        // ESTADO DE CAJA
        private string _estadoCaja = "Sin consultar";

        public string EstadoCaja
        {
            get => _estadoCaja;
            set => SetProperty(ref _estadoCaja, value);
        }

        // MENSAJES DEL SISTEMA
        private string _mensajeEstado =
            "Consultando información financiera...";

        public string MensajeEstado
        {
            get => _mensajeEstado;
            set => SetProperty(ref _mensajeEstado, value);
        }

        // ESTADO DE CARGA
        private bool _estaCargando;

        public bool EstaCargando
        {
            get => _estaCargando;
            set => SetProperty(ref _estaCargando, value);
        }

        // ACTUALIZAR PANEL FINANCIERO
        public async Task ActualizarAsync()
        {
            if (EstaCargando)
                return;

            EstaCargando = true;
            MensajeEstado = "Consultando información financiera...";

            try
            {
                var resumen =
                    await _api.ObtenerResumenFinancieroAsync();

                EstablecerResumenFinanciero(
                    resumen.VentasMes,
                    resumen.GastosMes,
                    resumen.GananciasMes,
                    resumen.SaldoCaja,
                    resumen.EstadoCaja);

                if (!resumen.HayDatosCaja)
                {
                    EstadoCaja = "Sin registros";
                }
            }
            catch (Exception ex)
            {
                MensajeEstado =
                    "No se pudo consultar el resumen: " +
                    ex.Message;

                throw;
            }
            finally
            {
                EstaCargando = false;
            }
        }

        // REGISTRAR UN GASTO
        public async Task RegistrarGastoAsync(
            string concepto,
            decimal monto)
        {
            if (EstaCargando)
            {
                throw new InvalidOperationException(
                    "Hay una operación en proceso.");
            }

            if (string.IsNullOrWhiteSpace(concepto))
            {
                throw new ArgumentException(
                    "Debes escribir el concepto del gasto.");
            }

            if (monto <= 0)
            {
                throw new ArgumentException(
                    "El monto debe ser mayor que cero.");
            }

            EstaCargando = true;
            MensajeEstado = "Registrando gasto...";

            try
            {
                await _api.RegistrarGastoAsync(
                    concepto.Trim(),
                    monto);

                MensajeEstado = "Gasto registrado correctamente.";
            }
            catch (Exception ex)
            {
                MensajeEstado =
                    "No se pudo registrar el gasto: " +
                    ex.Message;

                throw;
            }
            finally
            {
                EstaCargando = false;
            }

            // Actualizar las tarjetas después de guardar.
            await ActualizarAsync();
        }

        // ESTABLECER RESUMEN FINANCIERO
        public void EstablecerResumenFinanciero(
            decimal ventas,
            decimal gastos,
            decimal ganancias,
            decimal saldo,
            string estado)
        {
            VentasMes = ventas.ToString("C2");
            GastosMes = gastos.ToString("C2");
            GananciaMes = ganancias.ToString("C2");
            SaldoCaja = saldo.ToString("C2");

            EstadoCaja = estado;

            MensajeEstado =
                "Resumen financiero actualizado.";
        }

        // NOTIFICAR CAMBIOS A LA INTERFAZ
        public event PropertyChangedEventHandler?
            PropertyChanged;

        private bool SetProperty<T>(
            ref T campo,
            T valor,
            [CallerMemberName] string? nombre = null)
        {
            if (Equals(campo, valor))
                return false;

            campo = valor;
            OnPropertyChanged(nombre);
            return true;
        }

        private void OnPropertyChanged(
            [CallerMemberName] string? propiedad = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propiedad));
        }
    }
}
