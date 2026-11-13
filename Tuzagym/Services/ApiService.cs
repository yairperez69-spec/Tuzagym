
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Devices;
using Tuzagym.Models;

namespace Tuzagym.Services
{
    public class ApiService : IApiService
    {
        private readonly HttpClient _httpClient;

        // =====================================
        // CONFIGURACIÓN DE CONEXIÓN
        // =====================================

        // Actualiza esta URL si reinicias el túnel rápido de Cloudflare.


        private const string UrlPublicaApi =
    "https://tuzagym-tuzagym-api.onrender.com/";


        private static readonly JsonSerializerOptions _jsonOptions =
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

        public string UltimoError { get; private set; }
            = string.Empty;

        public void LimpiarToken()
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }

        public async Task<byte[]> DescargarReporteFinancieroAsync()
        {
            using var response = await _httpClient.GetAsync(
                "api/reportes/financiero-pdf");

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsByteArrayAsync();
        }

        public ApiService()
        {
            string baseUrl = ObtenerBaseUrl();

            Debug.WriteLine($"TUZAGYM API URL: {baseUrl}");

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(
                    baseUrl,
                    UriKind.Absolute),

                Timeout = TimeSpan.FromSeconds(60)
            };
        }



        public static string ObtenerBaseUrl()
        {
            return UrlPublicaApi;
        }


        // =====================================
        // AUTENTICACIÓN JWT
        // =====================================

        public void ConfigurarToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException(
                    "No se recibió un token JWT válido.",
                    nameof(token));
            }

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    token.Trim());
        }


        public async Task<LoginResult> LoginAsync(
            string correo,
            string contrasena)
        {
            try
            {
                var request = new LoginRequest
                {
                    Correo = correo.Trim(),
                    Contrasena = contrasena
                };

                using var response =
                    await _httpClient.PostAsJsonAsync(
                        "api/auth/login",
                        request,
                        _jsonOptions);

                string contenido =
                    await response.Content.ReadAsStringAsync();

                Debug.WriteLine(
                    $"LOGIN HTTP: {(int)response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    return new LoginResult
                    {
                        Exito = false,
                        Mensaje =
                            $"Error del servidor HTTP {(int)response.StatusCode}. " +
                            "Revisa los Logs de Render."
                    };
                }

                if (string.IsNullOrWhiteSpace(contenido))
                {
                    return new LoginResult
                    {
                        Exito = false,
                        Mensaje = "La API devolvió una respuesta vacía."
                    };
                }

                var resultado =
                    JsonSerializer.Deserialize<LoginResult>(
                        contenido,
                        _jsonOptions);

                if (resultado?.Exito == true &&
                    !string.IsNullOrWhiteSpace(resultado.Token))
                {
                    ConfigurarToken(resultado.Token);
                }
                else
                {
                    LimpiarToken();
                }

                return resultado ?? new LoginResult
                {
                    Exito = false,
                    Mensaje = "La API devolvió datos no válidos."
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error LoginAsync: {ex}");

                return new LoginResult
                {
                    Exito = false,
                    Mensaje = $"Error al iniciar sesión: {ex.Message}"
                };
            }
        }


        // =====================================
        // ENTRENADOR: OBTENER SOCIOS
        // =====================================

        public async Task<List<SocioEntrenador>>
            ObtenerSociosAsync()
        {
            using var response =
                await _httpClient.GetAsync(
                    "api/rutinas/socios");

            response.EnsureSuccessStatusCode();

            return await response.Content
                .ReadFromJsonAsync<List<SocioEntrenador>>(
                    _jsonOptions)
                ?? new List<SocioEntrenador>();
        }

        // =====================================
        // ENTRENADOR: ASIGNAR RUTINA
        // =====================================

        public async Task<bool> AsignarRutinaAsync(
            AsignarRutinaDto dto)
        {
            try
            {
                using var response =
                    await _httpClient.PostAsJsonAsync(
                        "api/rutinas/asignar",
                        dto,
                        _jsonOptions);

                string contenido =
                    await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(
                        $"HTTP {(int)response.StatusCode}\n\n" +
                        contenido);
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"Error al asignar rutina: {ex}");

                throw;
            }
        }

        // =====================================
        // ENTRENADOR: RUTINAS ASIGNADAS
        // =====================================

        public async Task<List<RutinaAsignadaDto>>
            ObtenerRutinasAsignadasAsync(int idEntrenador)
        {
            if (idEntrenador <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(idEntrenador));
            }

            using var response =
                await _httpClient.GetAsync(
                    $"api/rutinas/asignadas/{idEntrenador}");

            response.EnsureSuccessStatusCode();

            return await response.Content
                .ReadFromJsonAsync<List<RutinaAsignadaDto>>(
                    _jsonOptions)
                ?? new List<RutinaAsignadaDto>();
        }

        // =====================================
        // SOCIO: UNA RUTINA
        // =====================================

        public async Task<RutinaSocioDto>
            ObtenerRutinaSocioAsync(int idUsuario)
        {
            if (idUsuario <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(idUsuario));
            }

            using var response =
                await _httpClient.GetAsync(
                    $"api/rutinas/socio/{idUsuario}");

            response.EnsureSuccessStatusCode();

            return await response.Content
                .ReadFromJsonAsync<RutinaSocioDto>(
                    _jsonOptions)
                ?? throw new InvalidOperationException(
                    "La API devolvió una respuesta vacía.");
        }



        // =====================================
        // SOCIO: TODAS LAS RUTINAS
        // =====================================

        public async Task<RutinasSocioRespuestaDto>
            ObtenerTodasRutinasSocioAsync(int idUsuario)
        {
            if (idUsuario <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(idUsuario));
            }

            string ruta =
                $"api/rutinas/socio/{idUsuario}/todas";

            Debug.WriteLine(
                $"Consultando rutinas del usuario: {idUsuario}");

            using var response =
                await _httpClient.GetAsync(ruta);

            string contenido =
                await response.Content.ReadAsStringAsync();

            Debug.WriteLine(
                $"Rutinas HTTP {(int)response.StatusCode}");

            Debug.WriteLine(
                $"Respuesta de rutinas: {contenido}");

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Error HTTP {(int)response.StatusCode}: " +
                    contenido);
            }

            var resultado =
                JsonSerializer.Deserialize<RutinasSocioRespuestaDto>(
                    contenido,
                    _jsonOptions);

            return resultado
                ?? throw new InvalidOperationException(
                    "La API no devolvió información válida de las rutinas.");
        }

        // =====================================
        // RECEPCIÓN: REGISTRAR SOCIO
        // =====================================

        public async Task<RegistroSocioResult>
            RegistrarSocioAsync(RegistroSocioDto dto)
        {
            try
            {
                using var response =
                    await _httpClient.PostAsJsonAsync(
                        "api/recepcion/registrar-socio",
                        dto,
                        _jsonOptions);

                var resultado = await response.Content
                    .ReadFromJsonAsync<RegistroSocioResult>(
                        _jsonOptions);

                return resultado ?? new RegistroSocioResult
                {
                    Exito = false,
                    Mensaje =
                        "El servidor no devolvió una respuesta válida."
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"Error RegistrarSocioAsync: {ex}");

                return new RegistroSocioResult
                {
                    Exito = false,
                    Mensaje = $"Error de red: {ex.Message}"
                };
            }
        }

        // =====================================
        // RECEPCIÓN: VERIFICAR CÓDIGO
        // =====================================

        public async Task<bool> VerificarCodigoAsync(
            string correo,
            string codigo)
        {
            try
            {
                var dto = new ConfirmarCodigoDto
                {
                    Correo = correo,
                    CodigoVerificacion = codigo
                };

                using var response =
                    await _httpClient.PostAsJsonAsync(
                        "api/recepcion/verificar-codigo",
                        dto,
                        _jsonOptions);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"Error VerificarCodigoAsync: {ex}");

                return false;
            }
        }

        // =====================================
        // RECEPCIÓN: OBTENER ENTRENADORES
        // =====================================

        public async Task<List<EntrenadorDto>?>
            ObtenerEntrenadoresAsync()
        {
            try
            {
                using var response =
                    await _httpClient.GetAsync(
                        "api/recepcion/entrenadores");

                string contenido =
                    await response.Content.ReadAsStringAsync();

                Debug.WriteLine(
                    $"ENTRENADORES HTTP: {(int)response.StatusCode}");

                Debug.WriteLine(
                    $"ENTRENADORES JSON: {contenido}");

                response.EnsureSuccessStatusCode();

                var entrenadores =
                    JsonSerializer.Deserialize<List<EntrenadorDto>>(
                        contenido,
                        _jsonOptions)
                    ?? new List<EntrenadorDto>();

                Debug.WriteLine(
                    $"TOTAL ENTRENADORES RECIBIDOS: {entrenadores.Count}");

                foreach (var entrenador in entrenadores)
                {
                    Debug.WriteLine(
                        $"Entrenador: {entrenador.NombreCompleto}");
                }

                return entrenadores;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"ERROR OBTENIENDO ENTRENADORES: {ex}");

                throw;
            }
        }

        // =====================================
        // RECEPCIÓN: RESUMEN DE MEMBRESÍAS
        // =====================================

        public async Task<ResumenMembresiasDto?>
            ObtenerResumenMembresiasAsync()
        {
            using var response =
                await _httpClient.GetAsync(
                    "api/recepcion/resumen-membresias");

            response.EnsureSuccessStatusCode();

            return await response.Content
                .ReadFromJsonAsync<ResumenMembresiasDto>(
                    _jsonOptions)
                ?? throw new InvalidOperationException(
                    "La API no devolvió el resumen de membresías.");
        }

        // =====================================
        // RECEPCIÓN: CONSULTAR SOCIOS
        // =====================================

        public async Task<List<SocioRecepcionDto>>
            ObtenerSociosRecepcionAsync()
        {
            using var response =
                await _httpClient.GetAsync(
                    "api/recepcion/socios");

            response.EnsureSuccessStatusCode();

            return await response.Content
                .ReadFromJsonAsync<List<SocioRecepcionDto>>(
                    _jsonOptions)
                ?? new List<SocioRecepcionDto>();
        }

        // =====================================
        // RECEPCIÓN: ASIGNAR ENTRENADOR
        // =====================================

        public async Task<bool>
            AsignarEntrenadorRecepcionAsync(
                int idUsuario,
                int? idEntrenadorPersonal)
        {
            if (idUsuario <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(idUsuario));
            }

            var datos = new
            {
                IdEntrenadorPersonal = idEntrenadorPersonal
            };

            using var response =
                await _httpClient.PutAsJsonAsync(
                    $"api/recepcion/socios/{idUsuario}/entrenador",
                    datos,
                    _jsonOptions);

            response.EnsureSuccessStatusCode();

            return true;
        }

        // =====================================
        // RECEPCIÓN: RENOVAR MEMBRESÍA
        // =====================================

        public async Task<bool> RenovarMembresiaAsync(
            int idUsuario,
            string tipoMembresia,
            string metodoPago,
            decimal montoPagado)
        {
            if (idUsuario <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(idUsuario));
            }

            if (string.IsNullOrWhiteSpace(tipoMembresia))
            {
                throw new ArgumentException(
                    "Selecciona una membresía.",
                    nameof(tipoMembresia));
            }

            string tipo = tipoMembresia.Trim();

            if (tipo != "Normal" && tipo != "VIP")
            {
                throw new ArgumentException(
                    "La membresía debe ser Normal o VIP.",
                    nameof(tipoMembresia));
            }

            if (string.IsNullOrWhiteSpace(metodoPago))
            {
                throw new ArgumentException(
                    "Selecciona un método de pago.",
                    nameof(metodoPago));
            }

            string metodo = metodoPago.Trim();

            if (!metodo.Equals("Efectivo",
                    StringComparison.OrdinalIgnoreCase) &&
                !metodo.Equals("Tarjeta",
                    StringComparison.OrdinalIgnoreCase) &&
                !metodo.Equals("Transferencia",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "Método de pago no válido.",
                    nameof(metodoPago));
            }

            if (montoPagado <= 0 ||
                montoPagado > 99999999.99m ||
                decimal.Round(montoPagado, 2) != montoPagado)
            {
                throw new ArgumentException(
                    "El monto debe ser positivo y tener máximo dos decimales.",
                    nameof(montoPagado));
            }

            var datos = new
            {
                TipoMembresia = tipo,
                MetodoPago = metodo,
                MontoPagado = montoPagado
            };

            using var response =
                await _httpClient.PostAsJsonAsync(
                    $"api/recepcion/socios/{idUsuario}/renovar",
                    datos,
                    _jsonOptions);

            response.EnsureSuccessStatusCode();

            return true;
        }

        // =====================================
        // COMENZAR ENTRENAMIENTO
        // =====================================

        public async Task<ResultadoEntrenamientoDto>
            IniciarEntrenamientoAsync(int idAsignacion)
        {
            if (idAsignacion <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(idAsignacion));
            }

            using var response =
                await _httpClient.PostAsync(
                    $"api/avance-diario/iniciar/{idAsignacion}",
                    null);

            response.EnsureSuccessStatusCode();

            return await response.Content
                .ReadFromJsonAsync<ResultadoEntrenamientoDto>(
                    _jsonOptions)
                ?? throw new InvalidOperationException(
                    "No se recibió respuesta del servidor.");
        }

        // =====================================
        // FINALIZAR ENTRENAMIENTO
        // =====================================

        public async Task<ResultadoEntrenamientoDto>
            FinalizarEntrenamientoAsync(int idSesion)
        {
            if (idSesion <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(idSesion));
            }

            using var response =
                await _httpClient.PutAsync(
                    $"api/avance-diario/finalizar/{idSesion}",
                    null);

            response.EnsureSuccessStatusCode();

            return await response.Content
                .ReadFromJsonAsync<ResultadoEntrenamientoDto>(
                    _jsonOptions)
                ?? throw new InvalidOperationException(
                    "No se recibió respuesta del servidor.");
        }

        // =====================================
        // HISTORIAL POR ASIGNACIÓN
        // =====================================

        public async Task<List<AvanceDiarioDto>>
            ObtenerHistorialEntrenamientoAsync(
                int idAsignacion)
        {
            if (idAsignacion <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(idAsignacion));
            }

            using var response =
                await _httpClient.GetAsync(
                    $"api/avance-diario/historial/{idAsignacion}");

            response.EnsureSuccessStatusCode();

            return await response.Content
                .ReadFromJsonAsync<List<AvanceDiarioDto>>(
                    _jsonOptions)
                ?? new List<AvanceDiarioDto>();
        }

        // =====================================
        // RECEPCIÓN: REGISTRAR ASISTENCIA QR
        // =====================================

        public async Task<ResultadoAsistenciaDto>
            RegistrarAsistenciaAsync(string codigoQr)
        {
            if (string.IsNullOrWhiteSpace(codigoQr))
            {
                return new ResultadoAsistenciaDto
                {
                    Exito = false,
                    Mensaje = "El código QR está vacío."
                };
            }

            try
            {
                var datos = new
                {
                    CodigoQr = codigoQr.Trim()
                };

                using var response =
                    await _httpClient.PostAsJsonAsync(
                        "api/asistencias/registrar",
                        datos,
                        _jsonOptions);

                var resultado = await response.Content
                    .ReadFromJsonAsync<ResultadoAsistenciaDto>(
                        _jsonOptions);

                return resultado ?? new ResultadoAsistenciaDto
                {
                    Exito = false,
                    Mensaje = response.IsSuccessStatusCode
                        ? "La API devolvió una respuesta vacía."
                        : $"Error HTTP {(int)response.StatusCode}."
                };
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine(ex);

                return new ResultadoAsistenciaDto
                {
                    Exito = false,
                    Mensaje = "No se pudo conectar con la API."
                };
            }
            catch (TaskCanceledException)
            {
                return new ResultadoAsistenciaDto
                {
                    Exito = false,
                    Mensaje = "Tiempo de espera agotado."
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);

                return new ResultadoAsistenciaDto
                {
                    Exito = false,
                    Mensaje = "Error al registrar la asistencia."
                };
            }
        }

        // =====================================
        // ADMINISTRADOR: REGISTRAR EMPLEADO
        // =====================================

        public async Task<RegistroEmpleadoResult>
            RegistrarEmpleadoAsync(RegistrarEmpleadoDto dto)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            using var response =
                await _httpClient.PostAsJsonAsync(
                    "api/admin/empleados",
                    dto,
                    _jsonOptions);

            string contenido =
                await response.Content.ReadAsStringAsync();

            RegistroEmpleadoResult? resultado = null;

            if (!string.IsNullOrWhiteSpace(contenido))
            {
                try
                {
                    resultado =
                        JsonSerializer.Deserialize<RegistroEmpleadoResult>(
                            contenido,
                            _jsonOptions);
                }
                catch (JsonException)
                {
                    // La API podría devolver otro formato de error.
                }
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    resultado?.Mensaje ??
                    $"HTTP {(int)response.StatusCode}: {contenido}");
            }

            return resultado ??
                throw new InvalidOperationException(
                    "La API devolvió una respuesta vacía.");
        }

        // =====================================
        // ADMINISTRADOR: OBTENER EMPLEADOS
        // =====================================

        public async Task<List<EmpleadoDto>>
            ObtenerEmpleadosAsync()
        {
            using var response =
                await _httpClient.GetAsync(
                    "api/admin/empleados");

            response.EnsureSuccessStatusCode();

            return await response.Content
                .ReadFromJsonAsync<List<EmpleadoDto>>(
                    _jsonOptions)
                ?? new List<EmpleadoDto>();
        }

        // =====================================
        // ADMINISTRADOR: FINANZAS
        // =====================================

        public async Task<ResumenFinancieroDto>
            ObtenerResumenFinancieroAsync()
        {
            using var response = await _httpClient.GetAsync(
                "api/finanzas/resumen");

            response.EnsureSuccessStatusCode();

            var resultado = await response.Content
                .ReadFromJsonAsync<ResumenFinancieroDto>(
                    _jsonOptions);

            return resultado
                ?? throw new InvalidOperationException(
                    "La API no devolvió datos financieros.");
        }

        // =====================================
        // ADMINISTRADOR: CAMBIAR ESTADO
        // =====================================

        public async Task<bool>
            CambiarEstadoEmpleadoAsync(
                int idUsuario,
                string estado)
        {
            if (idUsuario <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(idUsuario));
            }

            if (estado != "Activo" &&
                estado != "Inactivo")
            {
                throw new ArgumentException(
                    "El estado debe ser Activo o Inactivo.",
                    nameof(estado));
            }

            var datos = new
            {
                Estado = estado
            };

            using var response =
                await _httpClient.PutAsJsonAsync(
                    $"api/admin/empleados/{idUsuario}/estado",
                    datos,
                    _jsonOptions);

            response.EnsureSuccessStatusCode();

            return true;
        }

        // =====================================
        // ADMINISTRADOR: REGISTRAR GASTOS
        // =====================================

        public async Task<bool> RegistrarGastoAsync(
            string concepto,
            decimal monto)
        {
            if (string.IsNullOrWhiteSpace(concepto))
            {
                throw new ArgumentException(
                    "Debes ingresar el concepto del gasto.");
            }

            if (monto <= 0)
            {
                throw new ArgumentException(
                    "El monto debe ser mayor que cero.");
            }

            var datos = new
            {
                Concepto = concepto.Trim(),
                Monto = monto
            };

            using var response =
                await _httpClient.PostAsJsonAsync(
                    "api/finanzas/gastos",
                    datos,
                    _jsonOptions);

            response.EnsureSuccessStatusCode();

            return true;
        }

        // =====================================
        // CONSULTAR ARQUEO DE CAJA
        // =====================================

        public async Task<ArqueoCajaDto> ObtenerArqueoCajaAsync()
        {
            using var response =
                await _httpClient.GetAsync(
                    "api/finanzas/caja/arqueo");

            response.EnsureSuccessStatusCode();

            var arqueo = await response.Content
                .ReadFromJsonAsync<ArqueoCajaDto>(
                    _jsonOptions);

            return arqueo ?? throw new InvalidOperationException(
                "La API no devolvió los datos del arqueo.");
        }

        // =====================================
        // CERRAR CAJA
        // =====================================

        public async Task<bool> CerrarCajaAsync()
        {
            using var response =
                await _httpClient.PostAsync(
                    "api/finanzas/caja/cerrar",
                    null);

            response.EnsureSuccessStatusCode();

            return true;
        }

        // =====================================
        // ABRIR CAJA
        // =====================================

        public async Task<bool> AbrirCajaAsync()
        {
            using var response =
                await _httpClient.PostAsync(
                    "api/finanzas/caja/abrir",
                    null);

            response.EnsureSuccessStatusCode();

            return true;
        }
    }
}
