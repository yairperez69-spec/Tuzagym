using System.Collections.Generic;
using System.Threading.Tasks;
using Tuzagym.Models;

namespace Tuzagym.Services
{
    public interface IApiService
    {
        // AUTENTICACIÓN
        Task<LoginResult> LoginAsync(string correo, string contrasena);
        void ConfigurarToken(string token);

        // ENTRENADOR
        Task<List<SocioEntrenador>> ObtenerSociosAsync();
        Task<bool> AsignarRutinaAsync(AsignarRutinaDto dto);
        Task<List<RutinaAsignadaDto>> ObtenerRutinasAsignadasAsync(int idEntrenador);

        // SOCIO
        Task<RutinaSocioDto> ObtenerRutinaSocioAsync(int idUsuario);
        Task<RutinasSocioRespuestaDto> ObtenerTodasRutinasSocioAsync(int idUsuario);

        // RECEPCIÓN
        Task<RegistroSocioResult> RegistrarSocioAsync(RegistroSocioDto dto);
        Task<bool> VerificarCodigoAsync(string correo, string codigo);
        Task<List<EntrenadorDto>?> ObtenerEntrenadoresAsync();
        Task<ResumenMembresiasDto?> ObtenerResumenMembresiasAsync();
        Task<List<SocioRecepcionDto>> ObtenerSociosRecepcionAsync();
        Task<bool> AsignarEntrenadorRecepcionAsync(int idUsuario, int? idEntrenadorPersonal);

        // NUEVO: RENOVACIÓN DE MEMBRESÍA
        Task<bool> RenovarMembresiaAsync(
            int idUsuario,
            string tipoMembresia,
            string metodoPago,
            decimal montoPagado);

        // ENTRENAMIENTOS
        Task<ResultadoEntrenamientoDto> IniciarEntrenamientoAsync(int idAsignacion);
        Task<ResultadoEntrenamientoDto> FinalizarEntrenamientoAsync(int idSesion);
        Task<List<AvanceDiarioDto>> ObtenerHistorialEntrenamientoAsync(int idAsignacion);

        // ASISTENCIA POR QR
        Task<ResultadoAsistenciaDto> RegistrarAsistenciaAsync(string codigoQr);

        // ADMINISTRADOR: EMPLEADOS
        Task<RegistroEmpleadoResult> RegistrarEmpleadoAsync(RegistrarEmpleadoDto dto);
        Task<List<EmpleadoDto>> ObtenerEmpleadosAsync();
        Task<bool> CambiarEstadoEmpleadoAsync(int idUsuario, string estado);

        // FINANZAS Y CAJA
        Task<ResumenFinancieroDto> ObtenerResumenFinancieroAsync();
        Task<bool> RegistrarGastoAsync(string concepto, decimal monto);
        Task<ArqueoCajaDto> ObtenerArqueoCajaAsync();
        Task<bool> CerrarCajaAsync();
        Task<bool> AbrirCajaAsync();
    }
}
