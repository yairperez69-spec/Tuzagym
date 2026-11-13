
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tuzagym.API.Data;

namespace Tuzagym.API.Controllers
{
    public class RegistrarAsistenciaDto
    {
        public string CodigoQr { get; set; } = string.Empty;
    }

    [ApiController]
    [Route("api/asistencias")]
    public class AsistenciasController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AsistenciasController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("registrar")]
        public async Task<IActionResult> Registrar(
            [FromBody] RegistrarAsistenciaDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CodigoQr))
            {
                return BadRequest(new
                {
                    exito = false,
                    mensaje = "El código QR está vacío."
                });
            }

            string codigo = dto.CodigoQr.Trim();

            var socio = await (
                from s in _context.Socios
                join u in _context.Usuarios
                    on s.IdUsuario equals u.IdUsuario
                where s.QrAcceso == codigo
                      && u.Rol == "Socio"
                select new
                {
                    u.IdUsuario,
                    u.Nombre,
                    u.Estado,
                    u.FechaVencimiento
                }
            ).FirstOrDefaultAsync();

            if (socio == null)
            {
                return NotFound(new
                {
                    exito = false,
                    mensaje = "Código QR no registrado."
                });
            }

            if (socio.Estado != "Activo")
            {
                return BadRequest(new
                {
                    exito = false,
                    mensaje = "El socio está inactivo."
                });
            }

            DateTime ahora = DateTime.Now;

            if (!socio.FechaVencimiento.HasValue ||
                socio.FechaVencimiento.Value < ahora)
            {
                return BadRequest(new
                {
                    exito = false,
                    mensaje = "La membresía está vencida."
                });
            }

            // Evitar registros repetidos en 60 segundos.
            DateTime limite = ahora.AddSeconds(-60);

            bool repetida = await _context.HistorialSesiones
                .AnyAsync(h =>
                    h.IdSocio == socio.IdUsuario &&
                    h.TipoEntrada == "Acceso QR" &&
                    h.FechaConsulta >= limite);

            if (repetida)
            {
                return Conflict(new
                {
                    exito = false,
                    mensaje = "La entrada ya fue registrada recientemente."
                });
            }

            var registro = new HistorialSesion
            {
                IdSocio = socio.IdUsuario,
                IdEntrenador = null,
                FechaConsulta = ahora,
                TipoEntrada = "Acceso QR"
            };

            _context.HistorialSesiones.Add(registro);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                exito = true,
                mensaje = "Acceso autorizado.",
                nombreSocio = socio.Nombre,
                fechaHora = ahora,
                idHistorial = registro.IdHistorial
            });
        }
    }
}
