
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using Tuzagym.Models;
using Tuzagym.Services;

namespace Tuzagym.Views
{
    public partial class EntrenadorHomePage : ContentPage

    {
        private readonly ApiService _apiService;
        private readonly ObservableCollection<SocioEntrenador> _alumnos;

        private LoginResult? _sesionEntrenador;
        private bool _cargandoAlumnos;
        private bool _guardandoRutina;
        private bool _consultandoEntrenamientos;

        public ObservableCollection<RutinaGestionItem> Rutinas
        { get; } = new();

        private RutinaGestionItem? _rutinaSeleccionada;
        private bool _procesandoCambio;

        public sealed class RutinaGestionItem
        {
            public int IdAsignacion { get; set; }
            public string Alumno { get; set; } = "";
            public string Nombre { get; set; } = "";
            public int Series { get; set; }
            public int Repeticiones { get; set; }
            public int DescansoSeg { get; set; }
            public string ComentarioEntrenador { get; set; } = "";
            public DateTime Fecha { get; set; }
            public string Resumen => $"{Series} series × {Repeticiones} repeticiones";
        }

        // ==========================================
        // RESPUESTA DEL CONTADOR DE LA API
        // ==========================================

        private sealed class EntrenandoAhoraRespuesta
        {
            public int TotalEntrenando { get; set; }
        }

        // ==========================================
        // CONSTRUCTOR PRINCIPAL
        // ==========================================

        public EntrenadorHomePage()
        {
            InitializeComponent();

            _apiService = new ApiService();

            _alumnos =
                new ObservableCollection<SocioEntrenador>();

            PickerSocio.ItemsSource = _alumnos;
            PickerSocio.ItemDisplayBinding =
                new Binding(nameof(SocioEntrenador.Nombre));

            ListaAlumnos.ItemsSource = _alumnos;
            ListaRutinas.ItemsSource = Rutinas;

            BindingContext = this;

            MostrarVista(true, false, false, false);
        }

        // ==========================================
        // CONSTRUCTOR CON SESIÓN DEL ENTRENADOR
        // ==========================================

        public EntrenadorHomePage(
            LoginResult sesionEntrenador) : this()
        {
            if (sesionEntrenador == null ||
                !sesionEntrenador.Exito ||
                sesionEntrenador.IdUsuario <= 0 ||
                !string.Equals(
                    sesionEntrenador.Rol,
                    "Entrenador",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "La sesión del entrenador no es válida.",
                    nameof(sesionEntrenador));
            }

            _sesionEntrenador = sesionEntrenador;
        }

        // ==========================================
        // CARGAR DATOS AL ABRIR LA PANTALLA
        // ==========================================

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await CargarAlumnosAsync();
            await CargarRutinasAsignadasAsync();
            await ActualizarEntrenamientosActivosAsync();
            await ActualizarContadorAlumnosAsync();
        }

        // Carga rutinas activas guardadas en SQL Server mediante la API.
        private async Task CargarRutinasAsignadasAsync()
        {
            if (_sesionEntrenador?.IdUsuario is not int idEntrenador || idEntrenador <= 0)
            {
                LblProgresoRutinas.Text = "—";
                return;
            }

            try
            {
                var asignadas = await _apiService.ObtenerRutinasAsignadasAsync(idEntrenador);
                Rutinas.Clear();
                foreach (var asignada in asignadas)
                {
                    Rutinas.Add(new RutinaGestionItem
                    {
                        IdAsignacion = asignada.IdAsignacion,
                        DescansoSeg = asignada.DescansoSeg,
                        ComentarioEntrenador = asignada.ComentarioEntrenador ?? "",
                        Alumno = asignada.Alumno,
                        Nombre = asignada.Nombre,
                        Series = asignada.Series,
                        Repeticiones = asignada.Repeticiones,
                        Fecha = asignada.Fecha
                    });
                }
                if (_rutinaSeleccionada != null)
                {
                    var actualizada = Rutinas.FirstOrDefault(r => r.IdAsignacion == _rutinaSeleccionada.IdAsignacion);
                    if (actualizada != null) SeleccionarRutina(actualizada);
                    else { _rutinaSeleccionada = null; PanelEditarRutina.IsVisible = false; }
                }
                LblTotalRutinas.Text = Rutinas.Select(r => r.IdAsignacion).Distinct().Count().ToString();
                // Una fila por ejercicio: contar asignaciones únicas, no ejercicios.
                LblProgresoRutinas.Text = asignadas
                    .Select(a => a.IdAsignacion)
                    .Distinct()
                    .Count()
                    .ToString();
            }
            catch (Exception ex)
            {
                LblProgresoRutinas.Text = "—";
                System.Diagnostics.Debug.WriteLine("Error cargando rutinas: " + ex);
            }
        }

        private sealed class TotalAlumnosRespuesta
        {
            public int TotalAlumnos { get; set; }
        }

        private async Task ActualizarContadorAlumnosAsync()
        {
            if (_sesionEntrenador == null ||
                _sesionEntrenador.IdUsuario <= 0)
            {
                LblTotalAlumnos.Text = "—";
                return;
            }

            try
            {
                using var cliente = CrearClienteEdicion();

                var datos = await cliente.GetFromJsonAsync<TotalAlumnosRespuesta>(
                    $"api/rutinas/entrenador/" +
                    $"{_sesionEntrenador.IdUsuario}/total-alumnos");

                LblTotalAlumnos.Text =
                    datos?.TotalAlumnos.ToString() ?? "—";
            }
            catch (Exception ex)
            {
                LblTotalAlumnos.Text = "—";
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }


        // ==========================================
        // CONTADOR DE ENTRENAMIENTOS EN PROCESO
        // ==========================================

        private async Task ActualizarEntrenamientosActivosAsync()
        {
            if (_consultandoEntrenamientos)
                return;

            _consultandoEntrenamientos = true;

            try
            {
                string baseUrl;

                if (DeviceInfo.Platform == DevicePlatform.Android)
                {
                    // Requiere adb reverse tcp:5192 tcp:5192
                    baseUrl = "http://10.0.2.2:5192/";
                }
                else
                {
                    baseUrl = "http://127.0.0.1:5192/";
                }

                using var cliente = new HttpClient
                {
                    BaseAddress = new Uri(baseUrl),
                    Timeout = TimeSpan.FromSeconds(15)
                };

                using var respuesta = await cliente.GetAsync(
                    "api/avance-diario/entrenando-ahora");

                respuesta.EnsureSuccessStatusCode();

                var datos = await respuesta.Content
                    .ReadFromJsonAsync<EntrenandoAhoraRespuesta>(
                        new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                if (datos == null)
                {
                    throw new InvalidOperationException(
                        "La API no devolvió el contador.");
                }

                LblAlumnosActivos.Text =
                    datos.TotalEntrenando.ToString();

                System.Diagnostics.Debug.WriteLine(
                    "Entrenamientos en proceso: " +
                    datos.TotalEntrenando);
            }
            catch (Exception ex)
            {
                LblAlumnosActivos.Text = "—";

                System.Diagnostics.Debug.WriteLine(
                    "Error al consultar entrenamientos: " +
                    ex.Message);
            }
            finally
            {
                _consultandoEntrenamientos = false;
            }
        }

        // ==========================================
        // CARGAR SOCIOS DESDE LA API
        // ==========================================

        private async Task CargarAlumnosAsync()
        {
            if (_cargandoAlumnos)
                return;

            _cargandoAlumnos = true;

            try
            {
                var socios =
                    await _apiService.ObtenerSociosAsync();

                _alumnos.Clear();

                foreach (var socio in socios)
                {
                    _alumnos.Add(socio);
                }

                ListaAlumnos.ItemsSource = _alumnos;
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Error al cargar socios",
                    $"No se pudieron consultar los socios: {ex.Message}",
                    "Aceptar");
            }
            finally
            {
                _cargandoAlumnos = false;
            }
        }

        // ==========================================
        // NAVEGACIÓN
        // ==========================================

        private void MostrarVista(
            bool inicio,
            bool alumnos,
            bool rutinas,
            bool progreso)
        {
            VistaInicio.IsVisible = inicio;
            VistaAlumnos.IsVisible = alumnos;
            VistaRutinas.IsVisible = rutinas;
            VistaProgreso.IsVisible = progreso;

            ActualizarMenu(inicio ? "Inicio" :
                alumnos ? "Alumnos" :
                rutinas ? "Rutinas" : "Progreso");
        }

        private async void OnInicioClicked(
            object sender, EventArgs e)
        {
            MostrarVista(true, false, false, false);

            await ActualizarEntrenamientosActivosAsync();
        }

        private void OnAlumnosClicked(
            object sender, EventArgs e)
        {
            MostrarVista(false, true, false, false);
        }

        private async void OnRutinasClicked(
            object sender, EventArgs e)
        {
            MostrarVista(false, false, true, false);
            await CargarRutinasAsignadasAsync();
        }

        private async void OnProgresoClicked(
            object sender, EventArgs e)
        {
            MostrarVista(false, false, false, true);
            await CargarRutinasAsignadasAsync();
            await ActualizarEntrenamientosActivosAsync();
        }

        // ==========================================
        // BUSCAR SOCIOS
        // ==========================================

        private void OnBuscarAlumnoChanged(
            object sender,
            TextChangedEventArgs e)
        {
            string busqueda =
                e.NewTextValue?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(busqueda))
            {
                ListaAlumnos.ItemsSource = _alumnos;
                return;
            }

            ListaAlumnos.ItemsSource = _alumnos
                .Where(s =>
                    s.Nombre.Contains(
                        busqueda,
                        StringComparison.OrdinalIgnoreCase) ||
                    s.Correo.Contains(
                        busqueda,
                        StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        private void ActualizarMenu(string seccion)
        {
            // Buscar por nombre evita referencias ambiguas a campos XAML generados.
            var inicio = this.FindByName<Button>("BtnInicio");
            var alumnos = this.FindByName<Button>("BtnAlumnos");
            var rutinas = this.FindByName<Button>("BtnRutinas");
            var progreso = BtnTabProgreso;

            foreach (var boton in new[] { inicio, alumnos, rutinas, progreso })
            {
                if (boton == null) continue;
                boton.BackgroundColor = Colors.Transparent;
                boton.TextColor = Color.FromArgb("#F1F5F9");
            }

            Button? activo = seccion switch
            {
                "Inicio" => inicio,
                "Alumnos" => alumnos,
                "Rutinas" => rutinas,
                "Progreso" => progreso,
                _ => inicio
            };

            if (activo != null)
            {
                activo.BackgroundColor = Color.FromArgb("#12352F");
                activo.TextColor = Color.FromArgb("#19D98B");
            }
        }

        // ==========================================
        // SELECCIONAR SOCIO
        // ==========================================

        private void OnAlumnoSeleccionado(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault()
                is not SocioEntrenador socio)
            {
                return;
            }

            PickerSocio.SelectedItem = socio;

            MostrarVista(true, false, false, false);

            if (sender is CollectionView lista)
            {
                lista.SelectedItem = null;
            }

            // Actualizar el contador al volver a Inicio
            _ = ActualizarEntrenamientosActivosAsync();
        }

        // ==========================================
        // GUARDAR Y ASIGNAR RUTINA
        // ==========================================

        private async void OnAsignarRutinaClicked(
            object sender, EventArgs e)
        {
            if (_guardandoRutina)
                return;

            if (_sesionEntrenador == null ||
                _sesionEntrenador.IdUsuario <= 0 ||
                !string.Equals(
                    _sesionEntrenador.Rol,
                    "Entrenador",
                    StringComparison.OrdinalIgnoreCase))
            {
                await DisplayAlert(
                    "Sesión inválida",
                    "Inicia sesión nuevamente como entrenador.",
                    "Aceptar");

                return;
            }

            if (PickerSocio.SelectedItem
                is not SocioEntrenador socio)
            {
                await DisplayAlert(
                    "Selecciona un socio",
                    "Debes seleccionar al socio que recibirá la rutina.",
                    "Aceptar");

                return;
            }

            string nombre =
                EntryNombreRutina.Text?.Trim()
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(nombre))
            {
                await DisplayAlert(
                    "Datos incompletos",
                    "Escribe el nombre de la rutina.",
                    "Aceptar");

                return;
            }

            if (!int.TryParse(
                EntrySeries.Text,
                out int series) ||
                series < 1 ||
                series > 20)
            {
                await DisplayAlert(
                    "Series inválidas",
                    "Escribe entre 1 y 20 series.",
                    "Aceptar");

                return;
            }

            if (!int.TryParse(
                EntryRepeticiones.Text,
                out int repeticiones) ||
                repeticiones <= 0)
            {
                await DisplayAlert(
                    "Repeticiones inválidas",
                    "Escribe un número entero positivo.",
                    "Aceptar");

                return;
            }

            var dto = new AsignarRutinaDto
            {
                IdUsuario = socio.IdUsuario,
                IdEntrenador =
                    _sesionEntrenador.IdUsuario,
                NombreRutina = nombre,
                Series = series,
                Repeticiones =
                    repeticiones.ToString()
            };

            _guardandoRutina = true;

            try
            {
                bool guardado =
                    await _apiService.AsignarRutinaAsync(dto);

                if (!guardado)
                {
                    await DisplayAlert(
                        "No se pudo guardar",
                        "La API rechazó la asignación. " +
                        "Revisa la respuesta HTTP en " +
                        "la ventana Salida de Visual Studio.",
                        "Aceptar");

                    return;
                }

                await CargarRutinasAsignadasAsync();

                EntryNombreRutina.Text = string.Empty;
                EntrySeries.Text = string.Empty;
                EntryRepeticiones.Text = string.Empty;

                await DisplayAlert(
                    "Rutina asignada",
                    $"La rutina '{nombre}' se asignó " +
                    $"correctamente a {socio.Nombre}.",
                    "Aceptar");
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Error al guardar",
                    ex.Message,
                    "Aceptar");
            }
            finally
            {
                _guardandoRutina = false;
            }
        }

        // ==========================================
        // GESTIÓN DE RUTINAS (ANDROID / WINDOWS)
        // ==========================================

        private void SeleccionarRutina(RutinaGestionItem rutina)
        {
            _rutinaSeleccionada = rutina;
            PanelEditarRutina.IsVisible = true;
            LblRutinaSeleccionada.Text = $"{rutina.Alumno} • Asignación #{rutina.IdAsignacion}";
            EditarNombre.Text = rutina.Nombre;
            EditarSeries.Text = rutina.Series.ToString();
            EditarRepeticiones.Text = rutina.Repeticiones.ToString();
            EditarDescanso.Text = rutina.DescansoSeg.ToString();
            EditarComentario.Text = rutina.ComentarioEntrenador;
        }

        private async void OnRutinaSeleccionada(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is not RutinaGestionItem rutina)
                return;
            SeleccionarRutina(rutina);
            MostrarVista(false, false, true, false);
            ListaRutinas.SelectedItem = null;
            await ScrollEntrenador.ScrollToAsync(0, 0, true);
        }

        private void OnGestionRutinaSeleccionada(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is RutinaGestionItem rutina)
                SeleccionarRutina(rutina);
        }

        private void OnCancelarEdicionClicked(object sender, EventArgs e)
        {
            _rutinaSeleccionada = null;
            PanelEditarRutina.IsVisible = false;
            ListaGestionRutinas.SelectedItem = null;
        }

        // Para el emulador Android usa 10.0.2.2. Para un teléfono real
        // configura la IP local del equipo que ejecuta la API.
        private static HttpClient CrearClienteEdicion()
        {
            string url = DeviceInfo.Platform == DevicePlatform.Android
                ? "http://10.0.2.2:5192/"
                : "http://127.0.0.1:5192/";
            return new HttpClient
            {
                BaseAddress = new Uri(url),
                Timeout = TimeSpan.FromSeconds(20)
            };
        }

        private static async Task VerificarRespuestaAsync(HttpResponseMessage respuesta)
        {
            if (respuesta.IsSuccessStatusCode) return;
            string detalle = await respuesta.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Error HTTP {(int)respuesta.StatusCode}: {detalle}");
        }

        private async void OnGuardarCambiosRutinaClicked(object sender, EventArgs e)
        {
            if (_procesandoCambio || _rutinaSeleccionada == null) return;
            string nombre = EditarNombre.Text?.Trim() ?? "";
            if (nombre.Length is < 1 or > 100 ||
                !int.TryParse(EditarSeries.Text, out int series) || series is < 1 or > 20 ||
                !int.TryParse(EditarRepeticiones.Text, out int reps) || reps < 1 ||
                !int.TryParse(EditarDescanso.Text, out int descanso) || descanso < 0)
            {
                await DisplayAlert("Datos inválidos", "Verifica nombre, series, repeticiones y descanso.", "Aceptar");
                return;
            }

            _procesandoCambio = true;
            try
            {
                using var cliente = CrearClienteEdicion();
                using var respuesta = await cliente.PutAsJsonAsync(
                    $"api/rutinas/editar/{_rutinaSeleccionada.IdAsignacion}",
                    new { Nombre = nombre, Series = series, Repeticiones = reps, DescansoSeg = descanso });
                await VerificarRespuestaAsync(respuesta);
                await CargarRutinasAsignadasAsync();
                await DisplayAlert("Actualizado", "La rutina se modificó correctamente.", "Aceptar");
            }
            catch (Exception ex)
            {
                await DisplayAlert("No se pudo modificar", ex.Message, "Aceptar");
            }
            finally { _procesandoCambio = false; }
        }

        private async void OnGuardarComentarioClicked(object sender, EventArgs e)
        {
            if (_procesandoCambio || _rutinaSeleccionada == null) return;
            string comentario = EditarComentario.Text?.Trim() ?? "";
            if (comentario.Length > 500)
            {
                await DisplayAlert("Comentario demasiado largo", "Máximo 500 caracteres.", "Aceptar");
                return;
            }

            _procesandoCambio = true;
            try
            {
                using var cliente = CrearClienteEdicion();
                using var respuesta = await cliente.PutAsJsonAsync(
                    $"api/rutinas/asignacion/{_rutinaSeleccionada.IdAsignacion}/comentario",
                    new { Comentario = comentario });
                await VerificarRespuestaAsync(respuesta);
                await CargarRutinasAsignadasAsync();
                await DisplayAlert("Guardado", "El comentario ya está disponible para el socio.", "Aceptar");
            }
            catch (Exception ex)
            {
                await DisplayAlert("No se pudo guardar", ex.Message, "Aceptar");
            }
            finally { _procesandoCambio = false; }
        }

        private async void OnEliminarRutinaClicked(object sender, EventArgs e)
        {
            if (_procesandoCambio) return;
            var rutina = _rutinaSeleccionada ?? ListaRutinas.SelectedItem as RutinaGestionItem;
            if (rutina == null)
            {
                await DisplayAlert("Selecciona una rutina", "Elige la rutina que deseas desactivar.", "Aceptar");
                return;
            }
            bool confirmar = await DisplayAlert("Desactivar rutina",
                $"¿Desactivar '{rutina.Nombre}' para {rutina.Alumno}? Se conservará el historial.",
                "Desactivar", "Cancelar");
            if (!confirmar) return;

            _procesandoCambio = true;
            try
            {
                using var cliente = CrearClienteEdicion();
                using var respuesta = await cliente.DeleteAsync(
                    $"api/rutinas/asignacion/{rutina.IdAsignacion}");
                await VerificarRespuestaAsync(respuesta);
                _rutinaSeleccionada = null;
                PanelEditarRutina.IsVisible = false;
                ListaGestionRutinas.SelectedItem = null;
                ListaRutinas.SelectedItem = null;
                await CargarRutinasAsignadasAsync();
                await DisplayAlert("Completado", "La asignación quedó inactiva.", "Aceptar");
            }
            catch (Exception ex)
            {
                await DisplayAlert("No se pudo desactivar", ex.Message, "Aceptar");
            }
            finally { _procesandoCambio = false; }
        }

        // ==========================================
        // CERRAR SESIÓN
        // ==========================================

        private async void OnCerrarSesionClicked(
            object sender, EventArgs e)
        {
            bool confirmar = await DisplayAlert(
                "Cerrar sesión",
                "¿Deseas cerrar tu sesión?",
                "Sí",
                "Cancelar");

            if (!confirmar)
                return;

            _sesionEntrenador = null;

            if (Application.Current != null)
            {
                Application.Current.MainPage =
                    new NavigationPage(new LoginPage());
            }
        }
    }
}
