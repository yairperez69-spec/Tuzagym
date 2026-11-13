
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Tuzagym.Models;
using Tuzagym.Services;
using Tuzagym.Views;

namespace Tuzagym.ViewModels
{
    public class RecepcionHomeViewModel : INotifyPropertyChanged
    {
        private readonly IApiService _apiService;

        // =====================================
        // CAMPOS PRIVADOS
        // =====================================

        private string _nombreCompleto = string.Empty;
        private string _correo = string.Empty;
        private string _telefono = string.Empty;

        private string _tipoMembresiaSelected = "Normal";
        private string _metodoPagoSelected = "Efectivo";

        private decimal _montoPagado = 500;
        private bool _esVip;
        private bool _registrandoSocio;
        private bool _cargandoEstadisticas;

        private int _totalSocios;
        private int _membresiasVigentes;
        private int _proximasAVencer;
        private int _membresiasVencidas;

        private string _mensajeEstadisticas =
            "Cargando estadísticas...";

        private ObservableCollection<EntrenadorDto>
            _entrenadores = new();

        private EntrenadorDto? _entrenadorSeleccionado;

        // =====================================
        // CAMPOS DE GASTOS
        // =====================================

        private string _conceptoGasto = string.Empty;
        private string _montoGastoTexto = string.Empty;
        private bool _registrandoGasto;

        // =====================================
        // NOTIFICACIÓN DE CAMBIOS
        // =====================================

        public event PropertyChangedEventHandler?
            PropertyChanged;

        protected void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }

        // =====================================
        // PROPIEDADES DE SOCIO
        // =====================================

        public string NombreCompleto
        {
            get => _nombreCompleto;
            set
            {
                _nombreCompleto = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string Correo
        {
            get => _correo;
            set
            {
                _correo = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string Telefono
        {
            get => _telefono;
            set
            {
                _telefono = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string TipoMembresiaSelected
        {
            get => _tipoMembresiaSelected;
            set
            {
                _tipoMembresiaSelected = value ?? "Normal";
                OnPropertyChanged();

                EsVip = _tipoMembresiaSelected.Contains(
                    "VIP",
                    StringComparison.OrdinalIgnoreCase);

                MontoPagado = EsVip ? 800 : 500;
            }
        }

        public bool EsVip
        {
            get => _esVip;
            set
            {
                _esVip = value;
                OnPropertyChanged();
            }
        }

        public string MetodoPagoSelected
        {
            get => _metodoPagoSelected;
            set
            {
                _metodoPagoSelected = value ?? "Efectivo";
                OnPropertyChanged();
            }
        }

        public decimal MontoPagado
        {
            get => _montoPagado;
            set
            {
                _montoPagado = value;
                OnPropertyChanged();
            }
        }

        // =====================================
        // ENTRENADORES
        // =====================================

        public ObservableCollection<EntrenadorDto> Entrenadores
        {
            get => _entrenadores;
            set
            {
                _entrenadores = value;
                OnPropertyChanged();
            }
        }

        public EntrenadorDto? EntrenadorSeleccionado
        {
            get => _entrenadorSeleccionado;
            set
            {
                _entrenadorSeleccionado = value;
                OnPropertyChanged();
            }
        }

        // =====================================
        // ESTADÍSTICAS
        // =====================================

        public int TotalSocios
        {
            get => _totalSocios;
            set
            {
                _totalSocios = value;
                OnPropertyChanged();
            }
        }

        public int MembresiasVigentes
        {
            get => _membresiasVigentes;
            set
            {
                _membresiasVigentes = value;
                OnPropertyChanged();
            }
        }

        public int ProximasAVencer
        {
            get => _proximasAVencer;
            set
            {
                _proximasAVencer = value;
                OnPropertyChanged();
            }
        }

        public int MembresiasVencidas
        {
            get => _membresiasVencidas;
            set
            {
                _membresiasVencidas = value;
                OnPropertyChanged();
            }
        }

        public string MensajeEstadisticas
        {
            get => _mensajeEstadisticas;
            set
            {
                _mensajeEstadisticas = value;
                OnPropertyChanged();
            }
        }

        public bool RegistrandoSocio
        {
            get => _registrandoSocio;
            private set
            {
                _registrandoSocio = value;
                OnPropertyChanged();

                if (RegistrarSocioCommand is Command comando)
                    comando.ChangeCanExecute();
            }
        }

        // =====================================
        // PROPIEDADES DE GASTOS
        // =====================================

        public string ConceptoGasto
        {
            get => _conceptoGasto;
            set
            {
                _conceptoGasto = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string MontoGastoTexto
        {
            get => _montoGastoTexto;
            set
            {
                _montoGastoTexto = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public bool RegistrandoGasto
        {
            get => _registrandoGasto;
            private set
            {
                _registrandoGasto = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(NoRegistrandoGasto));

                if (RegistrarGastoCommand is Command comando)
                    comando.ChangeCanExecute();
            }
        }

        public bool NoRegistrandoGasto => !RegistrandoGasto;

        // =====================================
        // COMANDOS
        // =====================================

        public ICommand RegistrarSocioCommand { get; }
        public ICommand CargarEntrenadoresCommand { get; }
        public ICommand CargarEstadisticasCommand { get; }
        public ICommand CerrarSesionCommand { get; }
        public ICommand SimularEscaneoQrCommand { get; }
        public ICommand RegistrarGastoCommand { get; }
        public ICommand VerProximasAVencerCommand { get; }
        public ICommand VerMembresiasVencidasCommand { get; }
        public ICommand RenovarMembresiaCommand { get; }

        // =====================================
        // CONSTRUCTOR
        // =====================================

        public RecepcionHomeViewModel(IApiService apiService)
        {
            _apiService = apiService ??
                throw new ArgumentNullException(nameof(apiService));

            RegistrarSocioCommand = new Command(
                async () => await RegistrarSocioAsync(),
                () => !RegistrandoSocio);

            CargarEntrenadoresCommand = new Command(
                async () => await CargarEntrenadoresAsync());

            CargarEstadisticasCommand = new Command(
                async () => await CargarEstadisticasAsync());

            CerrarSesionCommand = new Command(
                async () => await CerrarSesionAsync());

            SimularEscaneoQrCommand = new Command(
                async () => await AbrirEscanerAsync());

            RegistrarGastoCommand = new Command(
                async () => await RegistrarGastoRecepcionAsync(),
                () => !RegistrandoGasto);

            VerProximasAVencerCommand = new Command(
                async () => await MostrarSociosPorEstadoAsync(false));

            VerMembresiasVencidasCommand = new Command(
                async () => await MostrarSociosPorEstadoAsync(true));

            RenovarMembresiaCommand = new Command(
                async () => await SeleccionarSocioParaRenovarAsync());

            _ = InicializarAsync();
        }

        // =====================================
        // INICIALIZAR RECEPCIÓN
        // =====================================

        private async Task InicializarAsync()
        {
            await CargarEntrenadoresAsync();
            await CargarEstadisticasAsync();
        }

        // =====================================
        // CARGAR ENTRENADORES
        // =====================================

        private async Task CargarEntrenadoresAsync()
        {
            try
            {
                var lista =
                    await _apiService.ObtenerEntrenadoresAsync();

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Entrenadores =
                        new ObservableCollection<EntrenadorDto>(
                            lista ?? new());

                    EntrenadorSeleccionado = null;
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"Error cargando entrenadores: {ex}");
            }
        }

        // =====================================
        // CARGAR ESTADÍSTICAS
        // =====================================

        public async Task CargarEstadisticasAsync()
        {
            if (_cargandoEstadisticas)
                return;

            _cargandoEstadisticas = true;
            MensajeEstadisticas = "Actualizando estadísticas...";

            try
            {
                var resumen = await _apiService
                    .ObtenerResumenMembresiasAsync();

                if (resumen == null)
                {
                    MensajeEstadisticas =
                        "No se recibieron estadísticas.";
                    return;
                }

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    TotalSocios = resumen.TotalSocios;
                    MembresiasVigentes = resumen.MembresiasVigentes;
                    ProximasAVencer = resumen.ProximasAVencer;
                    MembresiasVencidas = resumen.MembresiasVencidas;

                    MensajeEstadisticas =
                        "Estadísticas actualizadas.";
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"ERROR ESTADÍSTICAS TUZAGYM: {ex}");

                MensajeEstadisticas =
                    "Error: " + ex.Message;
            }
            finally
            {
                _cargandoEstadisticas = false;
            }
        }

        // =====================================
        // REGISTRAR SOCIO
        // =====================================

        private async Task RegistrarSocioAsync()
        {
            if (RegistrandoSocio)
                return;

            if (string.IsNullOrWhiteSpace(NombreCompleto) ||
                string.IsNullOrWhiteSpace(Correo))
            {
                await MostrarAlertaAsync(
                    "Campos vacíos",
                    "Ingresa el nombre completo y el correo.");
                return;
            }

            if (!Correo.Contains("@") || !Correo.Contains("."))
            {
                await MostrarAlertaAsync(
                    "Correo inválido",
                    "Ingresa un correo electrónico válido.");
                return;
            }

            if (MontoPagado < 0)
            {
                await MostrarAlertaAsync(
                    "Monto inválido",
                    "El monto no puede ser negativo.");
                return;
            }

            var dto = new RegistroSocioDto
            {
                NombreCompleto = NombreCompleto.Trim(),
                Correo = Correo.Trim(),
                Telefono = Telefono.Trim(),
                TipoMembresia = TipoMembresiaSelected,
                IdEntrenadorPersonal = EntrenadorSeleccionado?.Id,
                MetodoPago = MetodoPagoSelected,
                MontoPagado = MontoPagado
            };

            RegistrandoSocio = true;

            try
            {
                var resultado =
                    await _apiService.RegistrarSocioAsync(dto);

                if (resultado != null && resultado.Exito)
                {
                    await MostrarAlertaAsync(
                        "Socio registrado",
                        resultado.Mensaje);

                    NombreCompleto = string.Empty;
                    Correo = string.Empty;
                    Telefono = string.Empty;

                    EntrenadorSeleccionado = null;
                    TipoMembresiaSelected = "Normal";
                    MetodoPagoSelected = "Efectivo";
                    MontoPagado = 500;

                    await CargarEstadisticasAsync();
                }
                else
                {
                    await MostrarAlertaAsync(
                        "Error al registrar",
                        resultado?.Mensaje ??
                        "No se pudo registrar al socio.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"Error registrando socio: {ex}");

                await MostrarAlertaAsync(
                    "Error de conexión",
                    "No se pudo confirmar el registro. " +
                    "Comprueba la conexión con la API.");
            }
            finally
            {
                RegistrandoSocio = false;
            }
        }

        // =====================================
        // ESCANEAR QR DE ASISTENCIA
        // =====================================

        private async Task AbrirEscanerAsync()
        {
            try
            {
                var pagina = Application.Current?.MainPage;

                if (pagina == null)
                    return;

                var escaner = new EscanearQrPage(
                    async codigo =>
                    {
                        var resultado =
                            await _apiService.RegistrarAsistenciaAsync(
                                codigo);

                        if (resultado.Exito)
                        {
                            return "ACCESO AUTORIZADO\n\n" +
                                   $"Socio: {resultado.NombreSocio}\n" +
                                   $"Fecha: {resultado.FechaHora:dd/MM/yyyy HH:mm}\n\n" +
                                   resultado.Mensaje;
                        }

                        return "ACCESO RECHAZADO\n\n" +
                               resultado.Mensaje;
                    });

                await pagina.Navigation.PushAsync(escaner);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"Error abriendo escáner: {ex}");

                await MostrarAlertaAsync(
                    "Error al abrir el escáner",
                    ex.Message);
            }
        }

        // =====================================
        // REGISTRAR GASTO DE RECEPCIÓN
        // =====================================

        private async Task RegistrarGastoRecepcionAsync()
        {
            if (RegistrandoGasto)
                return;

            string concepto = ConceptoGasto.Trim();

            if (concepto.Length < 3 || concepto.Length > 200)
            {
                await MostrarAlertaAsync(
                    "Concepto inválido",
                    "El concepto debe tener entre 3 y 200 caracteres.");
                return;
            }

            bool montoValido = decimal.TryParse(
                MontoGastoTexto,
                NumberStyles.Number,
                CultureInfo.CurrentCulture,
                out decimal monto);

            if (!montoValido)
            {
                montoValido = decimal.TryParse(
                    MontoGastoTexto,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out monto);
            }

            if (!montoValido ||
                monto <= 0 ||
                monto > 99999999.99m ||
                decimal.Round(monto, 2) != monto)
            {
                await MostrarAlertaAsync(
                    "Monto inválido",
                    "Ingresa un monto positivo con máximo dos decimales.");
                return;
            }

            var pagina = Application.Current?.MainPage;

            if (pagina == null)
                return;

            bool confirmar = await pagina.DisplayAlert(
                "Confirmar gasto",
                $"Concepto: {concepto}\n" +
                $"Monto: {monto:C2}\n\n" +
                "¿Deseas registrar este gasto en la caja abierta?",
                "Registrar",
                "Cancelar");

            if (!confirmar)
                return;

            RegistrandoGasto = true;

            try
            {
                if (_apiService is not ApiService servicio)
                {
                    throw new InvalidOperationException(
                        "El servicio API no es compatible.");
                }

                bool guardado = await servicio.RegistrarGastoAsync(
                    concepto,
                    monto);

                if (!guardado)
                {
                    await MostrarAlertaAsync(
                        "No se confirmó el registro",
                        "El servidor no confirmó el gasto. " +
                        "Revisa los movimientos antes de reintentar.");
                    return;
                }

                ConceptoGasto = string.Empty;
                MontoGastoTexto = string.Empty;

                await MostrarAlertaAsync(
                    "Gasto registrado",
                    "El gasto se guardó correctamente " +
                    "en los egresos de la caja abierta.");
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                Debug.WriteLine(
                    $"Error registrando gasto: {ex}");

                string mensaje = ex.StatusCode switch
                {
                    System.Net.HttpStatusCode.Unauthorized =>
                        "Tu sesión no es válida o ha expirado.",

                    System.Net.HttpStatusCode.Forbidden =>
                        "No tienes permiso para registrar gastos.",

                    System.Net.HttpStatusCode.Conflict =>
                        "No existe una caja abierta. " +
                        "Solicita al administrador abrir una.",

                    _ =>
                        "No se pudo confirmar el registro. " +
                        "Revisa los movimientos antes de reintentar."
                };

                await MostrarAlertaAsync(
                    "No se registró el gasto",
                    mensaje);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"Error registrando gasto: {ex}");

                await MostrarAlertaAsync(
                    "Error al registrar gasto",
                    "No se pudo confirmar la operación. " +
                    "Comprueba los movimientos y la conexión.");
            }
            finally
            {
                RegistrandoGasto = false;
            }
        }

        // =====================================
        // CONSULTAR VENCIMIENTOS Y RENOVAR
        // =====================================

        private async Task MostrarSociosPorEstadoAsync(bool vencidos)
        {
            try
            {
                var socios = await _apiService.ObtenerSociosRecepcionAsync();
                var filtrados = socios
                    .Where(s => vencidos
                        ? s.EstadoMembresia == "Vencida"
                        : s.EstadoMembresia == "Por vencer")
                    .OrderBy(s => s.FechaVencimiento)
                    .ToList();

                if (filtrados.Count == 0)
                {
                    await MostrarAlertaAsync("Membresías",
                        "No hay socios en esta categoría.");
                    return;
                }

                await MostrarSelectorSociosAsync(filtrados,
                    vencidos ? "Membresías vencidas" : "Próximas a vencer");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error consultando membresías: {ex}");
                await MostrarAlertaAsync("Error",
                    "No se pudo consultar la lista de socios.");
            }
        }

        private async Task SeleccionarSocioParaRenovarAsync()
        {
            try
            {
                var socios = await _apiService.ObtenerSociosRecepcionAsync();
                await MostrarSelectorSociosAsync(
                    socios.OrderBy(s => s.Nombre).ToList(),
                    "Selecciona el socio a renovar");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error consultando socios: {ex}");
                await MostrarAlertaAsync("Error",
                    "No se pudo consultar la lista de socios.");
            }
        }

        private async Task MostrarSelectorSociosAsync(
            List<SocioRecepcionDto> socios, string titulo)
        {
            var pagina = Application.Current?.MainPage;
            if (pagina == null) return;

            if (socios.Count == 0)
            {
                await MostrarAlertaAsync("Socios", "No hay socios disponibles.");
                return;
            }

            // Se incluye el ID para distinguir socios con nombres iguales.
            var opciones = socios.Select(s =>
                $"#{s.IdUsuario} - {s.Nombre} ({s.EstadoMembresia})").ToArray();

            string? elegido = await pagina.DisplayActionSheet(
                titulo, "Cancelar", null, opciones);

            int indice = Array.IndexOf(opciones, elegido);
            if (indice < 0) return;

            await MostrarDetalleSocioAsync(socios[indice]);
        }

        private async Task MostrarDetalleSocioAsync(SocioRecepcionDto socio)
        {
            var pagina = Application.Current?.MainPage;
            if (pagina == null) return;

            string fecha = socio.FechaVencimiento?.ToString("dd/MM/yyyy")
                ?? "Sin fecha";
            string dias = socio.DiasRestantes.HasValue
                ? socio.DiasRestantes.Value.ToString()
                : "No disponible";

            bool renovar = await pagina.DisplayAlert(
                "Detalle del socio",
                $"Nombre: {socio.Nombre}\n" +
                $"Correo: {socio.Correo}\n" +
                $"Teléfono: {socio.Telefono}\n" +
                $"Membresía: {socio.TipoMembresia}\n" +
                $"Estado: {socio.EstadoMembresia}\n" +
                $"Vence: {fecha}\n" +
                $"Días restantes: {dias}\n" +
                $"Entrenador: {socio.NombreEntrenador ?? "Sin asignar"}\n\n" +
                "¿Deseas renovar su membresía?",
                "Renovar", "Cerrar");

            if (renovar) await RenovarSocioAsync(socio);
        }

        private async Task RenovarSocioAsync(SocioRecepcionDto socio)
        {
            var pagina = Application.Current?.MainPage;
            if (pagina == null) return;

            string? tipo = await pagina.DisplayActionSheet(
                "Tipo de membresía", "Cancelar", null, "Normal", "VIP");
            if (tipo != "Normal" && tipo != "VIP") return;

            string? metodo = await pagina.DisplayActionSheet(
                "Método de pago", "Cancelar", null,
                "Efectivo", "Tarjeta", "Transferencia");
            if (metodo != "Efectivo" && metodo != "Tarjeta" &&
                metodo != "Transferencia") return;

            string? montoTexto = await pagina.DisplayPromptAsync(
                "Monto recibido", "Ingresa el importe de la renovación:",
                "Continuar", "Cancelar", "Monto en pesos",
                keyboard: Keyboard.Numeric,
                initialValue: tipo == "VIP" ? "800" : "500");

            if (montoTexto == null) return;
            if (!decimal.TryParse(montoTexto, NumberStyles.Number,
                    CultureInfo.CurrentCulture, out decimal monto) &&
                !decimal.TryParse(montoTexto, NumberStyles.Number,
                    CultureInfo.InvariantCulture, out monto))
            {
                await MostrarAlertaAsync("Monto inválido", "Ingresa un importe válido.");
                return;
            }

            if (monto <= 0 || monto > 99999999.99m ||
                decimal.Round(monto, 2) != monto)
            {
                await MostrarAlertaAsync("Monto inválido",
                    "El importe debe ser positivo y tener máximo dos decimales.");
                return;
            }

            bool confirmar = await pagina.DisplayAlert(
                "Confirmar renovación",
                $"Socio: {socio.Nombre}\nMembresía: {tipo}\n" +
                $"Pago: {metodo}\nMonto: {monto:C2}\n\n" +
                "Esta operación registrará un cobro en caja. ¿Continuar?",
                "Confirmar", "Cancelar");
            if (!confirmar) return;

            try
            {
                bool resultado = await _apiService.RenovarMembresiaAsync(
                    socio.IdUsuario, tipo, metodo, monto);

                if (resultado)
                {
                    await MostrarAlertaAsync("Renovación registrada",
                        "La API confirmó la renovación. Se actualizarán las estadísticas.");
                    await CargarEstadisticasAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error renovando membresía: {ex}");
                await MostrarAlertaAsync("Renovación no confirmada",
                    "Consulta los pagos y la vigencia del socio antes de reintentar " +
                    "para evitar un cobro duplicado.");
            }
        }

        // =====================================
        // CERRAR SESIÓN
        // =====================================

        private async Task CerrarSesionAsync()
        {
            var pagina = Application.Current?.MainPage;

            if (pagina == null)
                return;

            bool confirmar = await pagina.DisplayAlert(
                "Cerrar sesión",
                "¿Estás seguro de que deseas salir?",
                "Sí",
                "No");

            if (!confirmar)
                return;

            if (_apiService is ApiService servicio)
            {
                servicio.LimpiarToken();
            }

            Application.Current!.MainPage =
                new NavigationPage(new LoginPage());
        }

        // =====================================
        // MOSTRAR ALERTAS
        // =====================================

        private static Task MostrarAlertaAsync(
            string titulo,
            string mensaje)
        {
            return Application.Current?.MainPage?
                .DisplayAlert(
                    titulo,
                    mensaje,
                    "Aceptar")
                ?? Task.CompletedTask;
        }
    }
}
