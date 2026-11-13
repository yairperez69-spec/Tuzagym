
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using QRCoder;
using Tuzagym.Models;
using Tuzagym.Services;

namespace Tuzagym.ViewModels
{
    public class SocioHomeViewModel : INotifyPropertyChanged
    {
        private readonly IApiService _api;
        private readonly int _idUsuario;
        private bool _cargando;

        private string _mensajeRutinas =
            "Cargando rutinas...";

        public event PropertyChangedEventHandler?
            PropertyChanged;

        public string NombreSocio { get; }

        public string MembresiaInfo { get; }

        public string FolioQr { get; private set; }
            = "Código QR no disponible";

        public ImageSource? ImagenQr { get; private set; }

        public string MensajeRutinas
        {
            get => _mensajeRutinas;
            private set
            {
                _mensajeRutinas = value;
                Notificar();
            }
        }

        public string ResumenRutinas =>
            $"{Rutinas.Count} asignadas";

        public ObservableCollection<RutinaEntrenamientoItem>
            Rutinas
        { get; } = new();

        public Command CerrarSesionCommand { get; }

        public SocioHomeViewModel(
            LoginResult usuario,
            IApiService apiService)
        {
            if (usuario == null ||
                !usuario.Exito ||
                usuario.IdUsuario <= 0)
            {
                throw new ArgumentException(
                    "La sesión del socio no es válida.",
                    nameof(usuario));
            }

            _api = apiService ??
                throw new ArgumentNullException(
                    nameof(apiService));

            _idUsuario = usuario.IdUsuario;

            NombreSocio =
                string.IsNullOrWhiteSpace(usuario.Nombre)
                    ? "Socio TuzaGym"
                    : usuario.Nombre;

            string tipo =
                string.IsNullOrWhiteSpace(usuario.TipoMembresia)
                    ? "No especificada"
                    : usuario.TipoMembresia;

            string vence =
                usuario.FechaVencimiento?
                    .ToString("dd/MM/yyyy")
                ?? "No disponible";

            MembresiaInfo =
                $"Membresía {tipo} • Vence: {vence}";

            System.Diagnostics.Debug.WriteLine(
          $"QR recibido: {(string.IsNullOrWhiteSpace(usuario.CodigoQR) ? "VACÍO" : "CON DATOS")}");

            GenerarQr(usuario.CodigoQR);

            CerrarSesionCommand = new Command(
                async () => await CerrarSesionAsync());
        }

        private void Notificar(
            [CallerMemberName] string? nombre = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(nombre));
        }

        // =====================================
        // CARGAR TODAS LAS RUTINAS
        // =====================================

        public async Task CargarRutinaAsync()
        {
            if (_cargando)
                return;

            _cargando = true;

            MensajeRutinas =
                "Actualizando rutinas e historiales...";

            try
            {
                var respuesta = await _api
                    .ObtenerTodasRutinasSocioAsync(
                        _idUsuario);

                // Reutilizar los objetos existentes
                // para conservar el estado de sus comandos.
                var anteriores = Rutinas.ToDictionary(
                    r => r.IdAsignacion);

                var nuevos =
                    new List<RutinaEntrenamientoItem>();

                foreach (var dto in respuesta.Rutinas
                    ?? new List<RutinaAsignadaSocioDto>())
                {
                    if (dto.IdAsignacion <= 0)
                        continue;

                    if (!anteriores.TryGetValue(
                        dto.IdAsignacion,
                        out var item))
                    {
                        item = new RutinaEntrenamientoItem(
                            dto,
                            _api);
                    }
                    else
                    {
                        item.ActualizarDatos(dto);
                    }

                    nuevos.Add(item);
                }

                Rutinas.Clear();

                foreach (var item in nuevos)
                {
                    Rutinas.Add(item);
                }

                Notificar(nameof(ResumenRutinas));

                MensajeRutinas = Rutinas.Count == 0
                    ? "No tienes rutinas asignadas. Consulta a tu entrenador."
                    : "Selecciona una rutina para comenzar o finalizar su entrenamiento.";

                // Consultar cada historial por separado.
                foreach (var item in Rutinas)
                {
                    await item.VerificarHistorialAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"Error cargando rutinas: {ex}");

                MensajeRutinas =
                    "No se pudieron actualizar las rutinas: " +
                    ex.Message;
            }
            finally
            {
                _cargando = false;
            }
        }

        // =====================================
        // GENERAR QR PERSONAL
        // =====================================

        private void GenerarQr(string? codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                return;

            try
            {
                using var generador =
                    new QRCodeGenerator();

                using var datos =
                    generador.CreateQrCode(
                        codigo,
                        QRCodeGenerator.ECCLevel.Q);

                byte[] imagen =
                    new PngByteQRCode(datos)
                        .GetGraphic(12);

                ImagenQr = ImageSource.FromStream(
                    () => new MemoryStream(imagen));

                FolioQr = $"FOLIO: {codigo}";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error QR: {ex}");
            }
        }

        // =====================================
        // CERRAR SESIÓN
        // =====================================

        private static async Task CerrarSesionAsync()
        {
            var pagina =
                Application.Current?.MainPage;

            if (pagina == null)
                return;

            bool confirmar = await pagina.DisplayAlert(
                "Cerrar sesión",
                "¿Deseas salir de tu cuenta?",
                "Sí",
                "No");

            if (!confirmar)
                return;

            Application.Current!.MainPage =
                new NavigationPage(
                    new Views.LoginPage());
        }
    }

    // =========================================
    // ESTADO INDEPENDIENTE DE CADA RUTINA
    // =========================================

    public class RutinaEntrenamientoItem :
        INotifyPropertyChanged
    {
        private readonly IApiService _api;

        private int _idSesion;
        private bool _procesando;
        private bool _historialVerificado;

        private string _estado =
            "Verificando historial...";

        public event PropertyChangedEventHandler?
            PropertyChanged;

        public int IdAsignacion { get; }

        public string NombreRutina { get; private set; }
            = "";

        public string Descripcion { get; private set; }
            = "";

        // Observación específica de esta asignación de rutina.
        private string _comentarioEntrenador = "";

        public string ComentarioEntrenador
        {
            get => _comentarioEntrenador;
            private set
            {
                _comentarioEntrenador = value;
                Notificar();
                Notificar(nameof(TieneComentario));
            }
        }

        public bool TieneComentario =>
            !string.IsNullOrWhiteSpace(ComentarioEntrenador);

        public ObservableCollection<EjercicioSocioDto>
            Ejercicios
        { get; } = new();

        public string EstadoEntrenamiento
        {
            get => _estado;
            private set
            {
                _estado = value;
                Notificar();
            }
        }

        public bool PuedeIniciar =>
            _historialVerificado &&
            !_procesando &&
            _idSesion == 0;

        public bool PuedeFinalizar =>
            _historialVerificado &&
            !_procesando &&
            _idSesion > 0;

        public Command IniciarCommand { get; }

        public Command FinalizarCommand { get; }

        public RutinaEntrenamientoItem(
            RutinaAsignadaSocioDto dto,
            IApiService api)
        {
            _api = api ??
                throw new ArgumentNullException(
                    nameof(api));

            IdAsignacion = dto.IdAsignacion;

            IniciarCommand = new Command(
                async () => await IniciarAsync(),
                () => PuedeIniciar);

            FinalizarCommand = new Command(
                async () => await FinalizarAsync(),
                () => PuedeFinalizar);

            ActualizarDatos(dto);
        }

        private void Notificar(
            [CallerMemberName] string? nombre = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(nombre));
        }

        // =====================================
        // ACTUALIZAR INFORMACIÓN
        // =====================================

        public void ActualizarDatos(
            RutinaAsignadaSocioDto dto)
        {
            NombreRutina = dto.NombreRutina;

            Descripcion =
                $"Nivel: {dto.Nivel} • {dto.Objetivo}";

            ComentarioEntrenador = dto.ComentarioEntrenador ?? "";

            Ejercicios.Clear();

            foreach (var ejercicio in dto.Ejercicios
                ?? new List<EjercicioSocioDto>())
            {
                Ejercicios.Add(ejercicio);
            }

            Notificar(nameof(NombreRutina));
            Notificar(nameof(Descripcion));
        }

        private void RefrescarBotones()
        {
            Notificar(nameof(PuedeIniciar));
            Notificar(nameof(PuedeFinalizar));

            IniciarCommand.ChangeCanExecute();
            FinalizarCommand.ChangeCanExecute();
        }

        // =====================================
        // VERIFICAR HISTORIAL DE ESTA RUTINA
        // =====================================

        public async Task VerificarHistorialAsync()
        {
            if (_procesando)
                return;

            _historialVerificado = false;
            RefrescarBotones();

            try
            {
                var historial = await _api
                    .ObtenerHistorialEntrenamientoAsync(
                        IdAsignacion);

                var activa = historial
                    .Where(s => string.Equals(
                        s.Estado,
                        "En Proceso",
                        StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(s => s.FechaHora)
                    .FirstOrDefault();

                _idSesion = activa?.IdSesion ?? 0;
                _historialVerificado = true;

                EstadoEntrenamiento = activa == null
                    ? "Listo para comenzar"
                    : $"En proceso desde {activa.FechaHora:dd/MM/yyyy HH:mm}";
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"Error historial {IdAsignacion}: {ex}");

                EstadoEntrenamiento =
                    "No se pudo consultar el historial de esta rutina.";
            }
            finally
            {
                RefrescarBotones();
            }
        }

        // =====================================
        // COMENZAR ENTRENAMIENTO
        // =====================================

        private async Task IniciarAsync()
        {
            if (!PuedeIniciar)
                return;

            _procesando = true;
            RefrescarBotones();

            try
            {
                var resultado = await _api
                    .IniciarEntrenamientoAsync(
                        IdAsignacion);

                if (!resultado.Exito ||
                    resultado.IdSesion <= 0)
                {
                    throw new InvalidOperationException(
                        string.IsNullOrWhiteSpace(
                            resultado.Mensaje)
                            ? "La API no confirmó el inicio."
                            : resultado.Mensaje);
                }

                _idSesion = resultado.IdSesion;

                EstadoEntrenamiento =
                    "Entrenamiento en proceso";

                await Alerta(
                    "Entrenamiento iniciado",
                    $"Se inició la rutina {NombreRutina}.");
            }
            catch (Exception ex)
            {
                _historialVerificado = false;

                EstadoEntrenamiento =
                    "No se pudo confirmar el inicio. Actualiza la sección Rutina.";

                await Alerta(
                    "Error al iniciar",
                    ex.Message);
            }
            finally
            {
                _procesando = false;
                RefrescarBotones();
            }
        }

        // =====================================
        // FINALIZAR ENTRENAMIENTO
        // =====================================

        private async Task FinalizarAsync()
        {
            if (!PuedeFinalizar)
                return;

            var pagina =
                Application.Current?.MainPage;

            if (pagina == null)
                return;

            bool confirmar = await pagina.DisplayAlert(
                "Finalizar entrenamiento",
                $"¿Terminaste {NombreRutina}?",
                "Sí",
                "Cancelar");

            if (!confirmar || !PuedeFinalizar)
                return;

            _procesando = true;
            RefrescarBotones();

            try
            {
                var resultado = await _api
                    .FinalizarEntrenamientoAsync(
                        _idSesion);

                if (!resultado.Exito)
                {
                    throw new InvalidOperationException(
                        resultado.Mensaje);
                }

                _idSesion = 0;

                EstadoEntrenamiento =
                    $"Entrenamiento completado • {DateTime.Now:dd/MM/yyyy HH:mm}";

                await Alerta(
                    "Entrenamiento completado",
                    $"Se guardó el progreso de {NombreRutina}.");
            }
            catch (Exception ex)
            {
                _historialVerificado = false;

                EstadoEntrenamiento =
                    "No se pudo confirmar la finalización. Actualiza la sección Rutina.";

                await Alerta(
                    "Error al finalizar",
                    ex.Message);
            }
            finally
            {
                _procesando = false;
                RefrescarBotones();
            }
        }

        private static Task Alerta(
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
