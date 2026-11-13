
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
    [Route("api/[controller]")]
    public class RutinasController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RutinasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET api/rutinas/socios
        [HttpGet("socios")]
        public async Task<IActionResult> ObtenerSocios()
        {
            var socios = await (
                from s in _context.Socios.AsNoTracking()
                join u in _context.Usuarios.AsNoTracking()
                    on s.IdUsuario equals u.IdUsuario
                where u.Rol == "Socio"
                orderby u.Nombre
                select new
                {
                    idUsuario = u.IdUsuario,
                    nombre = u.Nombre,
                    correo = u.Correo,
                    estado = u.Estado,
                    tipoMembresia = u.TipoMembresia
                }
            ).ToListAsync();

            return Ok(socios);
        }

        // POST api/rutinas/asignar
        // Modo de desarrollo local: IdEntrenador
        // proviene del cliente y aún no está autenticado.
        [HttpPost("asignar")]
        public async Task<IActionResult> AsignarRutina(
            [FromBody] AsignarRutinaDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            if (!int.TryParse(dto.Repeticiones, out int reps)
                || reps <= 0)
            {
                return BadRequest(new
                {
                    exito = false,
                    mensaje = "Las repeticiones deben ser un entero positivo."
                });
            }

            string nombre = dto.NombreRutina?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(nombre))
            {
                return BadRequest(new
                {
                    exito = false,
                    mensaje = "Indica el nombre de la rutina."
                });
            }

            // Validar al entrenador contra las tablas reales.
            bool entrenadorExiste = await _context.Usuarios
                .AsNoTracking()
                .AnyAsync(u =>
                    u.IdUsuario == dto.IdEntrenador &&
                    u.Rol == "Entrenador" &&
                    u.Estado == "Activo");

            if (!entrenadorExiste)
            {
                return BadRequest(new
                {
                    exito = false,
                    mensaje = "El entrenador no existe o está inactivo."
                });
            }

            int registroEntrenador = await _context.Database
                .SqlQuery<int>($"""
                    SELECT COUNT(*) AS Value
                    FROM dbo.Entrenadores
                    WHERE idUsuario = {dto.IdEntrenador}
                    """)
                .SingleAsync();

            if (registroEntrenador == 0)
            {
                return BadRequest(new
                {
                    exito = false,
                    mensaje = "El entrenador no está registrado en dbo.Entrenadores."
                });
            }

            bool usuarioSocio = await _context.Usuarios
                .AsNoTracking()
                .AnyAsync(u =>
                    u.IdUsuario == dto.IdUsuario &&
                    u.Rol == "Socio" &&
                    u.Estado == "Activo");

            bool registroSocio = await _context.Socios
                .AsNoTracking()
                .AnyAsync(s => s.IdUsuario == dto.IdUsuario);

            if (!usuarioSocio || !registroSocio)
            {
                return BadRequest(new
                {
                    exito = false,
                    mensaje = "El socio seleccionado no existe o está inactivo."
                });
            }

            await using var transaccion =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var rutina = new Rutina
                {
                    IdEntrenador = dto.IdEntrenador,
                    Nombre = nombre,
                    Objetivo = "Acondicionamiento físico",
                    Nivel = "Principiante"
                };

                _context.Rutinas.Add(rutina);
                await _context.SaveChangesAsync();

                var ejercicio = new Ejercicio
                {
                    IdRutina = rutina.IdRutina,
                    Nombre = nombre,
                    Series = dto.Series,
                    Repeticiones = reps,
                    DescansoSeg = 60
                };

                _context.Ejercicios.Add(ejercicio);
                await _context.SaveChangesAsync();

                // Mantener activas las rutinas anteriores del socio.
                var asignacion = new AsignacionRutina
                {
                    IdSocio = dto.IdUsuario,
                    IdRutina = rutina.IdRutina,
                    FechaAsignacion = DateTime.Today,
                    Estado = "Activa"
                };

                _context.AsignacionesRutinas.Add(asignacion);
                await _context.SaveChangesAsync();

                await transaccion.CommitAsync();

                return Ok(new
                {
                    exito = true,
                    mensaje = "Rutina guardada y asignada correctamente.",
                    idRutina = rutina.IdRutina,
                    idEjercicio = ejercicio.IdEjercicio,
                    idAsignacion = asignacion.IdAsignacion
                });
            }
            catch (DbUpdateException ex)
            {
                await transaccion.RollbackAsync();

                return StatusCode(500, new
                {
                    exito = false,
                    mensaje = "Error de base de datos al guardar la rutina.",
                    detalle = ex.InnerException?.Message
                        ?? ex.Message
                });
            }
            catch (Exception ex)
            {
                await transaccion.RollbackAsync();

                return StatusCode(500, new
                {
                    exito = false,
                    mensaje = "No se pudo guardar la rutina.",
                    detalle = ex.Message
                });
            }
        }

        // GET api/rutinas/asignadas/{idEntrenador}
        [HttpGet("asignadas/{idEntrenador:int}")]
        public async Task<IActionResult> ObtenerAsignadas(
            int idEntrenador)
        {
            if (idEntrenador <= 0)
                return BadRequest("Entrenador inválido.");

            var asignadas = await (
                from a in _context.AsignacionesRutinas.AsNoTracking()
                join r in _context.Rutinas.AsNoTracking()
                    on a.IdRutina equals r.IdRutina
                join u in _context.Usuarios.AsNoTracking()
                    on a.IdSocio equals u.IdUsuario
                join e in _context.Ejercicios.AsNoTracking()
                    on r.IdRutina equals e.IdRutina
                where r.IdEntrenador == idEntrenador
                      && a.Estado == "Activa"
                orderby a.FechaAsignacion descending,
                        a.IdAsignacion descending
                select new
                {
                    idAsignacion = a.IdAsignacion,
                    idRutina = r.IdRutina,
                    idSocio = a.IdSocio,
                    alumno = u.Nombre,
                    nombre = r.Nombre,
                    objetivo = r.Objetivo,
                    nivel = r.Nivel,
                    series = e.Series,
                    repeticiones = e.Repeticiones,
                    descansoSeg = e.DescansoSeg,
                    fecha = a.FechaAsignacion,
                    estado = a.Estado,
                    comentarioEntrenador = a.ComentarioEntrenador
                }
            ).ToListAsync();

            return Ok(asignadas);
        }

        // GET api/rutinas/socio/{idUsuario}/todas
        // Devuelve TODAS las rutinas activas y sus ejercicios.
        [HttpGet("socio/{idUsuario:int}/todas")]
        public async Task<IActionResult> ObtenerTodasLasRutinasSocio(int idUsuario)
        {
            if (idUsuario <= 0)
                return BadRequest(new { exito = false, mensaje = "Socio inválido." });

            bool existe = await _context.Socios.AsNoTracking()
                .AnyAsync(s => s.IdUsuario == idUsuario);

            if (!existe)
                return NotFound(new { exito = false, mensaje = "El socio no existe." });

            var asignadas = await (
                from a in _context.AsignacionesRutinas.AsNoTracking()
                join r in _context.Rutinas.AsNoTracking()
                    on a.IdRutina equals r.IdRutina
                where a.IdSocio == idUsuario && a.Estado == "Activa"
                orderby a.FechaAsignacion descending,
                        a.IdAsignacion descending

                select new
                {
                    a.IdAsignacion,
                    r.IdRutina,
                    NombreRutina = r.Nombre,
                    r.Objetivo,
                    r.Nivel,
                    a.FechaAsignacion,
                    a.ComentarioEntrenador
                }
            ).ToListAsync();

            var ids = asignadas.Select(a => a.IdRutina).ToList();
            var ejercicios = await _context.Ejercicios.AsNoTracking()
                .Where(e => ids.Contains(e.IdRutina))
                .OrderBy(e => e.IdEjercicio)
                .Select(e => new
                {
                    e.IdEjercicio,
                    e.IdRutina,
                    e.Nombre,
                    e.Series,
                    e.Repeticiones,
                    e.DescansoSeg
                })
                .ToListAsync();

            var resultado = asignadas.Select(a => new
            {
                a.IdAsignacion,
                a.IdRutina,
                a.NombreRutina,
                a.Objetivo,
                a.Nivel,
                a.FechaAsignacion,
                a.ComentarioEntrenador,
                Ejercicios = ejercicios.Where(e => e.IdRutina == a.IdRutina).ToList()
            }).ToList();

            return Ok(new
            {
                tieneRutinas = resultado.Count > 0,
                rutinas = resultado
            });
        }


        [HttpGet("entrenador/{idEntrenador:int}/total-alumnos")]
        public async Task<IActionResult> ObtenerTotalAlumnosEntrenador(
            int idEntrenador)
        {
            if (idEntrenador <= 0)
                return BadRequest(new { mensaje = "Entrenador inválido." });

            int total = await (
                from a in _context.AsignacionesRutinas.AsNoTracking()
                join r in _context.Rutinas.AsNoTracking()
                    on a.IdRutina equals r.IdRutina
                join u in _context.Usuarios.AsNoTracking()
                    on a.IdSocio equals u.IdUsuario
                where r.IdEntrenador == idEntrenador
                      && a.Estado == "Activa"
                      && u.Rol == "Socio"
                      && u.Estado == "Activo"
                select a.IdSocio
            ).Distinct().CountAsync();

            return Ok(new
            {
                idEntrenador,
                totalAlumnos = total
            });
        }

        [HttpGet("socio/{idUsuario:int}")]
        public async Task<IActionResult> ObtenerRutinaSocio(
            int idUsuario)
        {
            if (idUsuario <= 0)
            {
                return BadRequest(new
                {
                    exito = false,
                    mensaje = "Socio inválido."
                });
            }

            bool existe = await _context.Socios
                .AsNoTracking()
                .AnyAsync(s => s.IdUsuario == idUsuario);

            if (!existe)
            {
                return NotFound(new
                {
                    exito = false,
                    mensaje = "El socio no existe."
                });
            }

            var rutina = await (
                from a in _context.AsignacionesRutinas.AsNoTracking()
                join r in _context.Rutinas.AsNoTracking()
                    on a.IdRutina equals r.IdRutina
                where a.IdSocio == idUsuario
                      && a.Estado == "Activa"
                orderby a.FechaAsignacion descending,
                        a.IdAsignacion descending
                select new
                {
                    idAsignacion = a.IdAsignacion,
                    idRutina = r.IdRutina,
                    nombreRutina = r.Nombre,
                    objetivo = r.Objetivo,
                    nivel = r.Nivel,
                    fechaAsignacion = a.FechaAsignacion,
                    comentarioEntrenador = a.ComentarioEntrenador
                }
            ).FirstOrDefaultAsync();

            if (rutina == null)
            {
                return Ok(new
                {
                    tieneRutina = false,
                    idAsignacion = 0,
                    idRutina = 0,
                    nombreRutina = "Sin rutina asignada",
                    descripcion = "",
                    objetivo = "",
                    comentarioEntrenador = (string?)null,
                    ejercicios = Array.Empty<object>()
                });
            }

            var ejercicios = await _context.Ejercicios
                .AsNoTracking()
                .Where(e => e.IdRutina == rutina.idRutina)
                .OrderBy(e => e.IdEjercicio)
                .Select(e => new
                {
                    idEjercicio = e.IdEjercicio,
                    nombre = e.Nombre,
                    series = e.Series,
                    repeticiones = e.Repeticiones,
                    descansoSeg = e.DescansoSeg
                })
                .ToListAsync();

            return Ok(new
            {
                tieneRutina = true,
                rutina.idAsignacion,
                rutina.idRutina,
                rutina.nombreRutina,
                descripcion = $"Nivel: {rutina.nivel}",
                rutina.objetivo,
                rutina.fechaAsignacion,
                rutina.comentarioEntrenador,
                ejercicios
            });
        }

        public class EditarRutinaDto
        {
            public string Nombre { get; set; } = "";
            public int Series { get; set; }
            public int Repeticiones { get; set; }
            public int DescansoSeg { get; set; }
        }

        [HttpPut("editar/{idAsignacion:int}")]
        public async Task<IActionResult> EditarRutina(
            int idAsignacion,
            [FromBody] EditarRutinaDto dto)
        {
            if (dto == null ||
                string.IsNullOrWhiteSpace(dto.Nombre) ||
                dto.Nombre.Length > 100 ||
                dto.Series <= 0 ||
                dto.Repeticiones <= 0 ||
                dto.DescansoSeg < 0)
            {
                return BadRequest(new
                {
                    mensaje = "Verifica los datos de la rutina."
                });
            }

            var asignacion = await _context.AsignacionesRutinas
                .FirstOrDefaultAsync(a =>
                    a.IdAsignacion == idAsignacion &&
                    a.Estado == "Activa");

            if (asignacion == null)
                return NotFound(new { mensaje = "Asignación no encontrada." });

            var rutina = await _context.Rutinas
                .FirstOrDefaultAsync(r =>
                    r.IdRutina == asignacion.IdRutina);

            if (rutina == null)
                return NotFound(new { mensaje = "Rutina no encontrada." });

            // Evitar modificar una rutina compartida por varios socios.
            bool compartida = await _context.AsignacionesRutinas
                .AnyAsync(a =>
                    a.IdRutina == rutina.IdRutina &&
                    a.IdAsignacion != idAsignacion);

            if (compartida)
                return Conflict(new
                {
                    mensaje = "Esta rutina tiene otras asignaciones. " +
                              "Debe crearse una copia antes de editarla."
                });

            var ejercicios = await _context.Ejercicios
                .Where(e => e.IdRutina == rutina.IdRutina)
                .ToListAsync();

            if (ejercicios.Count != 1)
                return Conflict(new
                {
                    mensaje = "Esta rutina no tiene exactamente un ejercicio. " +
                              "Se requiere edición individual de ejercicios."
                });

            rutina.Nombre = dto.Nombre.Trim();

            var ejercicio = ejercicios[0];
            ejercicio.Nombre = dto.Nombre.Trim();
            ejercicio.Series = dto.Series;
            ejercicio.Repeticiones = dto.Repeticiones;
            ejercicio.DescansoSeg = dto.DescansoSeg;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                exito = true,
                mensaje = "Rutina actualizada correctamente."
            });
        }

        [HttpDelete("asignacion/{idAsignacion:int}")]
        public async Task<IActionResult> DesactivarAsignacion(
            int idAsignacion)
        {
            var asignacion = await _context.AsignacionesRutinas
                .FirstOrDefaultAsync(a =>
                    a.IdAsignacion == idAsignacion &&
                    a.Estado == "Activa");

            if (asignacion == null)
                return NotFound(new
                {
                    mensaje = "Asignación activa no encontrada."
                });

            asignacion.Estado = "Inactiva";

            await _context.SaveChangesAsync();

            return Ok(new
            {
                exito = true,
                mensaje = "Asignación desactivada correctamente."
            });
        }

        public class GuardarComentarioRutinaDto
        {
            public string? Comentario { get; set; }
        }

        // PUT api/rutinas/asignacion/15/comentario
        [HttpPut("asignacion/{idAsignacion:int}/comentario")]
        public async Task<IActionResult> GuardarComentario(
            int idAsignacion,
            [FromBody] GuardarComentarioRutinaDto? dto)
        {
            if (dto == null)
            {
                return BadRequest(new
                {
                    exito = false,
                    mensaje = "Solicitud inválida."
                });
            }

            string comentario = dto.Comentario?.Trim() ?? "";

            if (comentario.Length > 500)
            {
                return BadRequest(new
                {
                    exito = false,
                    mensaje = "El comentario no puede superar 500 caracteres."
                });
            }

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

            asignacion.ComentarioEntrenador =
                string.IsNullOrWhiteSpace(comentario)
                    ? null
                    : comentario;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                exito = true,
                mensaje = "Comentario guardado correctamente.",
                comentarioEntrenador = asignacion.ComentarioEntrenador
            });
        }

    }
}

