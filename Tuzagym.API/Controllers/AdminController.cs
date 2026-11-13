
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tuzagym.API.Data;

namespace Tuzagym.API.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Administrador")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        private readonly PasswordHasher<Usuario>
            _passwordHasher = new();

        public AdminController(
            ApplicationDbContext context,
            ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ==========================================
        // REGISTRAR ENTRENADOR O RECEPCIONISTA
        // ==========================================

        [HttpPost("empleados")]
        public async Task<IActionResult> RegistrarEmpleado(
            [FromBody] RegistrarEmpleadoDto dto)
        {
            if (dto == null)
            {
                return BadRequest(new
                {
                    exito = false,
                    mensaje = "No se recibieron los datos."
                });
            }

            string rol = dto.Rol?.Trim() ?? "";

            if (rol != "Entrenador" &&
                rol != "Recepcionista")
            {
                return BadRequest(new
                {
                    exito = false,
                    mensaje = "Rol de empleado no válido."
                });
            }

            if (string.IsNullOrWhiteSpace(dto.Nombre) ||
                string.IsNullOrWhiteSpace(dto.Correo) ||
                string.IsNullOrWhiteSpace(dto.Contrasena))
            {
                return BadRequest(new
                {
                    exito = false,
                    mensaje = "Nombre, correo y contraseña son obligatorios."
                });
            }

            if (dto.Contrasena.Length < 8)
            {
                return BadRequest(new
                {
                    exito = false,
                    mensaje = "La contraseña debe tener al menos 8 caracteres."
                });
            }

            if (rol == "Entrenador" &&
                (string.IsNullOrWhiteSpace(dto.Especialidad) ||
                 dto.LimiteClientes is null or <= 0))
            {
                return BadRequest(new
                {
                    exito = false,
                    mensaje = "Indica la especialidad y un límite de clientes válido."
                });
            }

            if (rol == "Recepcionista" &&
                string.IsNullOrWhiteSpace(dto.Turno))
            {
                return BadRequest(new
                {
                    exito = false,
                    mensaje = "Indica el turno del recepcionista."
                });
            }

            string correo =
                dto.Correo.Trim().ToLowerInvariant();

            bool existe = await _context.Usuarios
                .AnyAsync(u => u.Correo.ToLower() == correo);

            if (existe)
            {
                return Conflict(new
                {
                    exito = false,
                    mensaje = "El correo ya está registrado."
                });
            }

            await using var transaccion =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // CREAR USUARIO
                var usuario = new Usuario
                {
                    Nombre = dto.Nombre.Trim(),
                    Correo = correo,
                    Telefono = dto.Telefono?.Trim(),
                    Rol = rol,
                    Estado = "Activo",
                    FechaCreacion = DateTime.UtcNow,

                    TipoMembresia = null,
                    FechaVencimiento = null,
                    MetodoPago = null,
                    MontoPagado = 0m,
                    IdEntrenadorPersonal = null
                };

                usuario.ContrasenaHash =
                    _passwordHasher.HashPassword(
                        usuario,
                        dto.Contrasena);

                _context.Usuarios.Add(usuario);

                await _context.SaveChangesAsync();

                // REGISTRAR INFORMACIÓN DEL EMPLEADO
                if (rol == "Entrenador")
                {
                    var entrenador = new Entrenador
                    {
                        IdUsuario = usuario.IdUsuario,
                        Especialidad =
                            dto.Especialidad!.Trim(),
                        LimiteClientes =
                            dto.LimiteClientes!.Value
                    };

                    _context.Entrenadores.Add(entrenador);
                }
                else
                {
                    var recepcionista = new Recepcionista
                    {
                        IdUsuario = usuario.IdUsuario,
                        Turno = dto.Turno!.Trim()
                    };

                    _context.Recepcionistas.Add(recepcionista);
                }

                await _context.SaveChangesAsync();

                await transaccion.CommitAsync();

                _logger.LogInformation(
                    "Empleado registrado. ID: {IdUsuario}, Rol: {Rol}",
                    usuario.IdUsuario,
                    rol);

                return Ok(new
                {
                    exito = true,
                    mensaje = "Empleado registrado correctamente.",
                    idUsuario = usuario.IdUsuario
                });
            }
            catch (DbUpdateException ex)
            {
                await transaccion.RollbackAsync();

                _logger.LogError(
                    ex,
                    "Error de SQL Server al registrar empleado. Rol: {Rol}",
                    rol);

                return Conflict(new
                {
                    exito = false,
                    mensaje =
                        "No se pudo guardar el empleado. " +
                        "Revisa el registro de errores de la API."
                });
            }
            catch (Exception ex)
            {
                await transaccion.RollbackAsync();

                _logger.LogError(
                    ex,
                    "Error inesperado al registrar empleado. Rol: {Rol}",
                    rol);

                return StatusCode(500, new
                {
                    exito = false,
                    mensaje =
                        "Ocurrió un error interno al registrar el empleado."
                });
            }
        }

        // ==========================================
        // CONSULTAR PERSONAL
        // ==========================================

        [HttpGet("empleados")]
        public async Task<IActionResult> ObtenerEmpleados()
        {
            var empleados = await _context.Usuarios
                .AsNoTracking()
                .Where(u =>
                    u.Rol == "Entrenador" ||
                    u.Rol == "Recepcionista")
                .OrderBy(u => u.Nombre)
                .Select(u => new
                {
                    idUsuario = u.IdUsuario,
                    nombre = u.Nombre,
                    correo = u.Correo,
                    telefono = u.Telefono,
                    rol = u.Rol,
                    estado = u.Estado,
                    fechaCreacion = u.FechaCreacion
                })
                .ToListAsync();

            return Ok(empleados);
        }

        // ==========================================
        // ACTIVAR O DESACTIVAR EMPLEADO
        // ==========================================

        [HttpPut("empleados/{id:int}/estado")]
        public async Task<IActionResult> CambiarEstado(
            int id,
            [FromBody] CambiarEstadoEmpleadoDto dto)
        {
            if (dto == null ||
                (dto.Estado != "Activo" &&
                 dto.Estado != "Inactivo"))
            {
                return BadRequest(new
                {
                    exito = false,
                    mensaje = "Estado no válido."
                });
            }

            var empleado = await _context.Usuarios
                .FirstOrDefaultAsync(u =>
                    u.IdUsuario == id &&
                    (u.Rol == "Entrenador" ||
                     u.Rol == "Recepcionista"));

            if (empleado == null)
            {
                return NotFound(new
                {
                    exito = false,
                    mensaje = "Empleado no encontrado."
                });
            }

            empleado.Estado = dto.Estado;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                exito = true,
                mensaje = "Estado actualizado correctamente."
            });
        }
    }

    // ==========================================
    // DTO PARA REGISTRAR EMPLEADOS
    // ==========================================

    public class RegistrarEmpleadoDto
    {
        [Required]
        [StringLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Correo { get; set; } = string.Empty;

        public string? Telefono { get; set; }

        [Required]
        public string Rol { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string Contrasena { get; set; } = string.Empty;

        public string? Especialidad { get; set; }

        public int? LimiteClientes { get; set; }

        public string? Turno { get; set; }
    }

    // ==========================================
    // DTO PARA CAMBIAR ESTADO
    // ==========================================

    public class CambiarEstadoEmpleadoDto
    {
        [Required]
        public string Estado { get; set; } = string.Empty;
    }
}
