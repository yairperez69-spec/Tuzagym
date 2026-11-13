
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Tuzagym.Models;
using Tuzagym.Services;
using Tuzagym.ViewModels;
using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Microsoft.Extensions.DependencyInjection;


namespace Tuzagym.Views
{
    public partial class AdminHomePage : ContentPage
    {
        private readonly AdminHomeViewModel _viewModel;
        private readonly ApiService _apiService;
        private bool _sesionConfigurada;

        // Constructor de compatibilidad.
        // Permite mantener llamadas antiguas a AdminHomePage().

        public AdminHomePage()
        {
            InitializeComponent();

            // Obtener el servicio compartido.
            _apiService = IPlatformApplication.Current?
                .Services.GetRequiredService<ApiService>()
                ?? throw new InvalidOperationException(
                    "No se pudo obtener ApiService.");

            _viewModel = new AdminHomeViewModel(_apiService);

            BindingContext = _viewModel;

            _sesionConfigurada = false;
        }

        // Constructor utilizado después del login.
        public AdminHomePage(LoginResult resultado) : this()
        {
            if (resultado == null ||
                !resultado.Exito ||
                string.IsNullOrWhiteSpace(resultado.Token))
            {
                return;
            }

            _apiService.ConfigurarToken(resultado.Token);

            _sesionConfigurada = true;
        }


        private async Task<bool> VerificarSesionAsync()
        {
            if (_sesionConfigurada)
                return true;

            await DisplayAlert(
                "Sesión requerida",
                "No se recibió el token JWT del administrador. " +
                "Cierra sesión y vuelve a iniciar sesión.",
                "Aceptar");

            return false;
        }

        private async Task MostrarErrorAsync(Exception ex)
        {
            string mensaje;

            if (ex is HttpRequestException httpEx &&
                httpEx.StatusCode == HttpStatusCode.Unauthorized)
            {
                mensaje = "La sesión no es válida o ha expirado. " +
                          "Vuelve a iniciar sesión.";
            }
            else if (ex is HttpRequestException forbidden &&
                     forbidden.StatusCode == HttpStatusCode.Forbidden)
            {
                mensaje = "Tu cuenta no tiene permiso para " +
                          "realizar esta operación.";
            }
            else
            {
                mensaje = ex.Message;
            }

            await DisplayAlert(
                "No se pudo completar la operación",
                mensaje,
                "Aceptar");
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (!_sesionConfigurada)
                return;

            try
            {
                await _viewModel.ActualizarAsync();
            }
            catch (Exception ex)
            {
                await MostrarErrorAsync(ex);
            }
        }

        // =========================================
        // ACTUALIZAR PANEL
        // =========================================

        private async void OnActualizarClicked(
            object sender, EventArgs e)
        {
            try
            {
                await _viewModel.ActualizarAsync();
            }
            catch (Exception ex)
            {
                await MostrarErrorAsync(ex);
            }
        }

        // =========================================
        // REGISTRAR ENTRENADOR
        // =========================================

        private async void OnRegistrarEntrenadorClicked(
            object sender, EventArgs e)
        {
            await RegistrarEmpleadoAsync("Entrenador");
        }

        // =========================================
        // REGISTRAR RECEPCIONISTA
        // =========================================

        private async void OnRegistrarRecepcionistaClicked(
            object sender, EventArgs e)
        {
            await RegistrarEmpleadoAsync("Recepcionista");
        }

        // =========================================
        // FORMULARIO DE CONTRATACIÓN
        // =========================================

        private async Task RegistrarEmpleadoAsync(string rol)
        {
            if (!await VerificarSesionAsync())
                return;

            try
            {
                string? nombre = await DisplayPromptAsync(
                    $"Nuevo {rol}",
                    "Nombre completo:",
                    "Siguiente",
                    "Cancelar",
                    "Nombre del empleado");

                if (nombre == null)
                    return;

                nombre = nombre.Trim();

                if (string.IsNullOrWhiteSpace(nombre))
                {
                    await DisplayAlert(
                        "Dato obligatorio",
                        "Debes ingresar el nombre.",
                        "Aceptar");
                    return;
                }

                string? correo = await DisplayPromptAsync(
                    $"Nuevo {rol}",
                    "Correo electrónico:",
                    "Siguiente",
                    "Cancelar",
                    "correo@ejemplo.com",
                    keyboard: Keyboard.Email);

                if (correo == null)
                    return;

                correo = correo.Trim();

                if (string.IsNullOrWhiteSpace(correo) ||
                    !correo.Contains('@'))
                {
                    await DisplayAlert(
                        "Correo inválido",
                        "Ingresa un correo electrónico válido.",
                        "Aceptar");
                    return;
                }

                string? telefono = await DisplayPromptAsync(
                    $"Nuevo {rol}",
                    "Número telefónico (opcional):",
                    "Siguiente",
                    "Cancelar",
                    "Teléfono",
                    keyboard: Keyboard.Telephone);

                if (telefono == null)
                    return;

                string? contrasena = await DisplayPromptAsync(
                    $"Nuevo {rol}",
                    "Contraseña inicial (mínimo 8 caracteres):",
                    "Siguiente",
                    "Cancelar",
                    "Contraseña",
                    maxLength: 100);

                if (contrasena == null)
                    return;

                if (contrasena.Length < 8)
                {
                    await DisplayAlert(
                        "Contraseña inválida",
                        "La contraseña debe tener al menos " +
                        "8 caracteres.",
                        "Aceptar");
                    return;
                }

                var empleado = new RegistrarEmpleadoDto
                {
                    Nombre = nombre,
                    Correo = correo,
                    Telefono = telefono.Trim(),
                    Rol = rol,
                    Contrasena = contrasena
                };

                if (rol == "Entrenador")
                {
                    string? especialidad =
                        await DisplayPromptAsync(
                            "Datos del entrenador",
                            "Especialidad:",
                            "Siguiente",
                            "Cancelar",
                            "Ej. Musculación");

                    if (especialidad == null)
                        return;

                    if (string.IsNullOrWhiteSpace(especialidad))
                    {
                        await DisplayAlert(
                            "Dato obligatorio",
                            "Ingresa la especialidad.",
                            "Aceptar");
                        return;
                    }

                    string? limiteTexto =
                        await DisplayPromptAsync(
                            "Datos del entrenador",
                            "Límite de clientes:",
                            "Siguiente",
                            "Cancelar",
                            "15",
                            keyboard: Keyboard.Numeric);

                    if (limiteTexto == null)
                        return;

                    if (!int.TryParse(
                            limiteTexto,
                            out int limite) ||
                        limite <= 0)
                    {
                        await DisplayAlert(
                            "Número inválido",
                            "Ingresa un límite mayor que cero.",
                            "Aceptar");
                        return;
                    }

                    empleado.Especialidad =
                        especialidad.Trim();

                    empleado.LimiteClientes = limite;
                }
                else
                {
                    string turno = await DisplayActionSheet(
                        "Selecciona el turno",
                        "Cancelar",
                        null,
                        "Matutino",
                        "Vespertino",
                        "Nocturno");

                    if (string.IsNullOrWhiteSpace(turno) ||
                        turno == "Cancelar")
                    {
                        return;
                    }

                    empleado.Turno = turno;
                }

                bool confirmar = await DisplayAlert(
                    "Confirmar contratación",
                    $"Nombre: {empleado.Nombre}\n" +
                    $"Correo: {empleado.Correo}\n" +
                    $"Rol: {empleado.Rol}\n\n" +
                    "¿Deseas registrar este empleado?",
                    "Registrar",
                    "Cancelar");

                if (!confirmar)
                    return;

                RegistroEmpleadoResult resultado =
                    await _apiService.RegistrarEmpleadoAsync(
                        empleado);

                if (resultado.Exito)
                {
                    await DisplayAlert(
                        "Registro exitoso",
                        string.IsNullOrWhiteSpace(resultado.Mensaje)
                            ? "El empleado se registró correctamente."
                            : resultado.Mensaje,
                        "Aceptar");
                }
                else
                {
                    await DisplayAlert(
                        "No se registró",
                        resultado.Mensaje,
                        "Aceptar");
                }
            }
            catch (Exception ex)
            {
                await MostrarErrorAsync(ex);
            }
        }

        // =========================================
        // CONSULTAR PERSONAL
        // =========================================

        private async void OnConsultarPersonalClicked(
            object sender, EventArgs e)
        {
            if (!await VerificarSesionAsync())
                return;

            try
            {
                List<EmpleadoDto> empleados =
                    await _apiService.ObtenerEmpleadosAsync();

                if (empleados.Count == 0)
                {
                    await DisplayAlert(
                        "Personal",
                        "No hay empleados registrados.",
                        "Aceptar");
                    return;
                }

                string[] opciones = empleados
                    .Select(x =>
                        $"{x.Nombre} | {x.Rol} | {x.Estado}")
                    .ToArray();

                string seleccion = await DisplayActionSheet(
                    "Selecciona un empleado",
                    "Cancelar",
                    null,
                    opciones);

                if (string.IsNullOrWhiteSpace(seleccion) ||
                    seleccion == "Cancelar")
                {
                    return;
                }

                int indice = Array.IndexOf(
                    opciones,
                    seleccion);

                if (indice < 0)
                    return;

                EmpleadoDto empleado = empleados[indice];

                await MostrarEmpleadoAsync(empleado);
            }
            catch (Exception ex)
            {
                await MostrarErrorAsync(ex);
            }
        }

        // =========================================
        // DETALLES DEL EMPLEADO
        // =========================================

        private async Task MostrarEmpleadoAsync(
            EmpleadoDto empleado)
        {
            string nuevoEstado =
                empleado.Estado == "Activo"
                    ? "Inactivo"
                    : "Activo";

            string accion =
                nuevoEstado == "Activo"
                    ? "Activar cuenta"
                    : "Desactivar cuenta";

            string opcion = await DisplayActionSheet(
                empleado.Nombre,
                "Cancelar",
                null,
                "Ver información",
                accion);

            if (opcion == "Ver información")
            {
                await DisplayAlert(
                    "Información del empleado",
                    $"ID: {empleado.IdUsuario}\n" +
                    $"Nombre: {empleado.Nombre}\n" +
                    $"Correo: {empleado.Correo}\n" +
                    $"Teléfono: {empleado.Telefono}\n" +
                    $"Rol: {empleado.Rol}\n" +
                    $"Estado: {empleado.Estado}",
                    "Aceptar");

                return;
            }

            if (opcion != accion)
                return;

            bool confirmar = await DisplayAlert(
                "Confirmar cambio",
                $"¿Deseas cambiar el estado de " +
                $"{empleado.Nombre} a {nuevoEstado}?",
                "Confirmar",
                "Cancelar");

            if (!confirmar)
                return;

            try
            {
                bool actualizado =
                    await _apiService.CambiarEstadoEmpleadoAsync(
                        empleado.IdUsuario,
                        nuevoEstado);

                if (actualizado)
                {
                    await DisplayAlert(
                        "Estado actualizado",
                        $"La cuenta de {empleado.Nombre} " +
                        $"ahora está {nuevoEstado}.",
                        "Aceptar");
                }
            }
            catch (Exception ex)
            {
                await MostrarErrorAsync(ex);
            }
        }

        // =========================================
        // CONTROL FINANCIERO
        // =========================================


        private bool _operacionCajaEnProceso = false;

        // =========================================
        // ARQUEO, CIERRE Y APERTURA DE CAJA
        // =========================================

        private async void OnCierreCajaClicked(
            object sender, EventArgs e)
        {
            if (_operacionCajaEnProceso)
                return;

            _operacionCajaEnProceso = true;

            try
            {
                if (!await VerificarSesionAsync())
                    return;

                if (_viewModel.EstaCargando)
                {
                    await DisplayAlert(
                        "Espera",
                        "Hay una operación financiera en proceso.",
                        "Aceptar");
                    return;
                }

                // Consultar el arqueo actual
                ArqueoCajaDto arqueo =
                    await _apiService.ObtenerArqueoCajaAsync();

                string detalle =
                    $"Caja: #{arqueo.IdCaja}\n" +
                    $"Fecha: {arqueo.Fecha:dd/MM/yyyy}\n" +
                    $"Estado: {arqueo.Estado}\n\n" +
                    $"Ingresos: {arqueo.TotalIngresos:C2}\n" +
                    $"Egresos: {arqueo.TotalEgresos:C2}\n" +
                    $"Saldo: {arqueo.SaldoActual:C2}";

                // Si la caja está abierta
                if (arqueo.Estado.Equals(
                    "Abierta",
                    StringComparison.OrdinalIgnoreCase))
                {
                    await DisplayAlert(
                      "ARQUEO DE CAJA",
                         detalle,
                         "Continuar");

                    string opcion = await DisplayActionSheet(
                        "Opciones de caja",
                        "Cancelar",
                        null,
                        "Cerrar caja");

                    if (opcion != "Cerrar caja")
                        return;

                    bool confirmar = await DisplayAlert(
                        "Confirmar cierre de caja",
                        detalle +
                        "\n\n¿Confirmas que deseas cerrar esta caja? " +
                        "Después del cierre no se podrán registrar " +
                        "nuevos movimientos en ella.",
                        "Sí, cerrar caja",
                        "Cancelar");

                    if (!confirmar)
                        return;

                    bool cerrada =
                        await _apiService.CerrarCajaAsync();

                    if (cerrada)
                    {
                        await _viewModel.ActualizarAsync();

                        await DisplayAlert(
                            "Caja cerrada",
                            $"La caja #{arqueo.IdCaja} " +
                            "se cerró correctamente.\n\n" +
                            "Puedes abrir una nueva caja " +
                            "desde este mismo módulo.",
                            "Aceptar");
                    }

                    return;
                }

                // Si la caja está cerrada
                if (arqueo.Estado.Equals(
                    "Cerrada",
                    StringComparison.OrdinalIgnoreCase))
                {
                    string opcion = await DisplayActionSheet(
                        "Último arqueo\n\n" + detalle,
                        "Cancelar",
                        null,
                        "Abrir nueva caja");

                    if (opcion != "Abrir nueva caja")
                        return;

                    bool confirmar = await DisplayAlert(
                        "Abrir nueva caja",
                        "Se creará una nueva caja con " +
                        "ingresos y egresos en $0.00.\n\n" +
                        "¿Deseas continuar?",
                        "Sí, abrir caja",
                        "Cancelar");

                    if (!confirmar)
                        return;

                    bool abierta =
                        await _apiService.AbrirCajaAsync();

                    if (abierta)
                    {
                        await _viewModel.ActualizarAsync();

                        await DisplayAlert(
                            "Caja abierta",
                            "La nueva caja se abrió correctamente.",
                            "Aceptar");
                    }

                    return;
                }

                await DisplayAlert(
                    "Estado desconocido",
                    $"La API devolvió el estado: {arqueo.Estado}",
                    "Aceptar");
            }
            catch (Exception ex)
            {
                await MostrarErrorAsync(ex);
            }
            finally
            {
                _operacionCajaEnProceso = false;
            }
        }


        // =========================================
        // ADMINISTRADOR: REGISTRAR GASTOS
        // =========================================

        private async void OnRegistrarGastosClicked(
            object sender, EventArgs e)
        {
            try
            {
                if (!await VerificarSesionAsync())
                    return;

                if (_viewModel.EstaCargando || _operacionCajaEnProceso)
                {
                    await DisplayAlert(
                        "Espera",
                        "Hay una operación financiera en proceso.",
                        "Aceptar");
                    return;
                }

                string concepto = await DisplayPromptAsync(
                    "Registrar gasto",
                    "Escribe el concepto del gasto:",
                    "Continuar",
                    "Cancelar",
                    "Ejemplo: Productos de limpieza",
                    maxLength: 150);

                if (string.IsNullOrWhiteSpace(concepto))
                    return;

                string montoTexto = await DisplayPromptAsync(
                    "Monto del gasto",
                    "Ingresa el monto en pesos:",
                    "Continuar",
                    "Cancelar",
                    "Ejemplo: 50.00",
                    keyboard: Keyboard.Numeric);

                if (string.IsNullOrWhiteSpace(montoTexto))
                    return;

                if (!decimal.TryParse(
                        montoTexto,
                        System.Globalization.NumberStyles.Number,
                        System.Globalization.CultureInfo.CurrentCulture,
                        out decimal monto) &&
                    !decimal.TryParse(
                        montoTexto,
                        System.Globalization.NumberStyles.Number,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out monto))
                {
                    await DisplayAlert(
                        "Monto inválido",
                        "Ingresa un monto numérico válido.",
                        "Aceptar");
                    return;
                }

                if (monto <= 0)
                {
                    await DisplayAlert(
                        "Monto inválido",
                        "El gasto debe ser mayor que cero.",
                        "Aceptar");
                    return;
                }

                bool confirmar = await DisplayAlert(
                    "Confirmar gasto",
                    $"Concepto: {concepto.Trim()}\n" +
                    $"Monto: {monto:C2}\n\n" +
                    "¿Deseas registrar este gasto?",
                    "Registrar",
                    "Cancelar");

                if (!confirmar)
                    return;

                await _viewModel.RegistrarGastoAsync(concepto.Trim(), monto);

                await DisplayAlert(
                    "Gasto registrado",
                    "El gasto se guardó correctamente en el sistema.",
                    "Aceptar");
            }
            catch (Exception ex)
            {
                await MostrarErrorAsync(ex);
            }
        }


        private async void OnGananciasClicked(
            object sender, EventArgs e)
        {
            if (!await VerificarSesionAsync())
                return;

            try
            {
                var resumen =
                    await _apiService.ObtenerResumenFinancieroAsync();

                decimal ventas = resumen.VentasMes;
                decimal gastos = resumen.GastosMes;
                decimal ganancias = resumen.GananciasMes;

                string detalle =
                    $"PERIODO: {DateTime.Now:MMMM yyyy}\n\n" +
                    $"VENTAS DEL MES: {ventas:C2}\n\n" +
                    $"GASTOS DEL MES: {gastos:C2}\n\n" +
                    $"GANANCIA REGISTRADA: {ganancias:C2}\n\n" +
                    $"SALDO DE CAJA: {resumen.SaldoCaja:C2}\n" +
                    $"ESTADO DE CAJA: {resumen.EstadoCaja}";

                await DisplayAlert(
                    "GANANCIAS MENSUALES",
                    detalle,
                    "Aceptar");

                await _viewModel.ActualizarAsync();
            }
            catch (Exception ex)
            {
                await MostrarErrorAsync(ex);
            }
        }




        private async void OnReporteClicked(
            object sender, EventArgs e)
        {
            if (!await VerificarSesionAsync())
                return;

            try
            {
                string opcion = await DisplayActionSheet(
                    "REPORTES TUZAGYM",
                    "Cancelar",
                    null,
                    "Descargar reporte financiero PDF",
                    "Comisiones de entrenadores");

                if (opcion == "Comisiones de entrenadores")
                {
                    await DisplayAlert(
                        "Comisiones",
                        "Todavía debemos definir los porcentajes " +
                        "y la información de comisiones.",
                        "Aceptar");

                    return;
                }

                if (opcion != "Descargar reporte financiero PDF")
                    return;

                // SOLICITAR EL PDF AL SERVIDOR
                byte[] pdf =
                    await _apiService.DescargarReporteFinancieroAsync();

                // COMPROBAR QUE EL ARCHIVO ES UN PDF
                if (pdf == null ||
                    pdf.Length < 5 ||
                    System.Text.Encoding.ASCII.GetString(
                        pdf, 0, 5) != "%PDF-")
                {
                    await DisplayAlert(
                        "Reporte inválido",
                        "La API no devolvió un archivo PDF válido.",
                        "Aceptar");

                    return;
                }

                // CREAR NOMBRE Y RUTA TEMPORAL
                string nombre =
                    $"TuzaGym_Reporte_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

                string ruta = Path.Combine(
                    FileSystem.CacheDirectory,
                    nombre);

                // GUARDAR EL ARCHIVO RECIBIDO
                await File.WriteAllBytesAsync(ruta, pdf);

                // ABRIR OPCIONES DE GUARDAR O COMPARTIR
                await Share.Default.RequestAsync(
                    new ShareFileRequest
                    {
                        Title = "Reporte financiero TuzaGym",
                        File = new ShareFile(ruta)
                    });
            }
            catch (Exception ex)
            {
                await MostrarErrorAsync(ex);
            }
        }


        // NAVEGACIÓN INFERIOR: desplaza el panel sin abrir páginas nuevas.
        private async void OnNavInicioClicked(object sender, EventArgs e)
        {
            await PanelScroll.ScrollToAsync(0, 0, true);
        }

        private async void OnNavPersonalClicked(object sender, EventArgs e)
        {
            await PanelScroll.ScrollToAsync(TituloPersonal, ScrollToPosition.Start, true);
        }

        private async void OnNavFinanzasClicked(object sender, EventArgs e)
        {
            await PanelScroll.ScrollToAsync(TituloFinanzas, ScrollToPosition.Start, true);
        }

        // =========================================
        // CERRAR SESIÓN
        // =========================================

        private async void OnCerrarSesionClicked(
            object sender, EventArgs e)
        {
            bool confirmar = await DisplayAlert(
                "Cerrar sesión",
                "¿Deseas salir de la cuenta de administrador?",
                "Sí",
                "Cancelar");

            if (!confirmar)
                return;

            _apiService.LimpiarToken();
            _sesionConfigurada = false;

            if (Application.Current != null)
            {
                Application.Current.MainPage =
                    new NavigationPage(new LoginPage());
            }
        }
    }
}
