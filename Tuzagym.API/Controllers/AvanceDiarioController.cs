
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tuzagym.API.Data;
using Tuzagym.API.Models;

namespace Tuzagym.API.Controllers
{
    [ApiController]
    [Route("api/avance-diario")]
    public class AvanceDiarioController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AvanceDiarioController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        // ======================================
        // CONSULTAR HISTORIAL DE UNA ASIGNACIÓN
        // ======================================

        [HttpGet("historial/{idAsignacion:int}")]
        public async Task<IActionResult> Historial(
            int idAsignacion)
        {
            var sesiones = await _context.AvancesDiarios
                .AsNoTracking()
                .Where(a =>
                    a.IdAsignacion == idAsignacion)
                .OrderByDescending(a => a.FechaHora)
                .Select(a => new
                {
                    a.IdSesion,
                    a.IdAsignacion,
                    a.FechaHora,
                    a.Estado,
                    a.FechaFinalizacion
                })
                .ToListAsync();

            return Ok(sesiones);
        }

        // ======================================
        // COMENZAR ENTRENAMIENTO
        // ======================================

        [HttpPost("iniciar/{idAsignacion:int}")]
        public async Task<IActionResult> Iniciar(
            int idAsignacion)
        {
            var asignacion = await _context.AsignacionesRutinas
                .FirstOrDefaultAsync(a =>
                    a.IdAsignacion == idAsignacion &&
                    a.Estado == "Activa");

            if (asignacion == null)
            {
                return NotFound(new
                {
                    exito = false,
                    mensaje = "Asignación activa no encontrada."
                });
            }

            var sesionActiva = await _context.AvancesDiarios
                .FirstOrDefaultAsync(a =>
                    a.IdAsignacion == idAsignacion &&
                    a.Estado == "En Proceso");

            if (sesionActiva != null)
            {
                return Conflict(new
                {
                    exito = false,
                    mensaje = "Ya existe un entrenamiento en proceso.",
                    idSesion = sesionActiva.IdSesion
                });
            }

            var sesion = new AvanceDiario
            {
                IdAsignacion = idAsignacion,
                FechaHora = DateTime.Now,
                Estado = "En Proceso"
            };

            _context.AvancesDiarios.Add(sesion);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                exito = true,
                mensaje = "Entrenamiento iniciado correctamente.",
                idSesion = sesion.IdSesion
            });
        }

        // ======================================
        // CONTAR ENTRENAMIENTOS EN PROCESO
        // ======================================

        [HttpGet("entrenando-ahora")]
        public async Task<IActionResult> ObtenerEntrenandoAhora()
        {
            var total = await _context.AvancesDiarios
                .AsNoTracking()
                .Where(a =>
                    a.Estado == "En Proceso")
                .Select(a => a.IdAsignacion)
                .Distinct()
                .CountAsync();

            return Ok(new
            {
                totalEntrenando = total
            });
        }

        // ======================================
        // FINALIZAR ENTRENAMIENTO
        // ======================================

        [HttpPut("finalizar/{idSesion:int}")]
        public async Task<IActionResult> Finalizar(
            int idSesion)
        {
            var sesion = await _context.AvancesDiarios
                .FirstOrDefaultAsync(a =>
                    a.IdSesion == idSesion);

            if (sesion == null)
            {
                return NotFound(new
                {
                    exito = false,
                    mensaje = "Sesión no encontrada."
                });
            }

            if (sesion.Estado != "En Proceso")
            {
                return BadRequest(new
                {
                    exito = false,
                    mensaje = "El entrenamiento no está en proceso."
                });
            }

            sesion.Estado = "Completada";
            sesion.FechaFinalizacion = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                exito = true,
                mensaje = "Entrenamiento finalizado correctamente.",
                idSesion = sesion.IdSesion
            });
        }
    }
}
