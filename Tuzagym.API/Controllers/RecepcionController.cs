
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tuzagym.API.Data;
using Tuzagym.API.Services;
using Microsoft.AspNetCore.Authorization;
namespace Tuzagym.API.Controllers
{
    // ==========================================
    // DTOs
    // ==========================================

    public class RegistroSocioDto
    {
        public string NombreCompleto { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string TipoMembresia { get; set; } = "Normal";
        public int? IdEntrenadorPersonal { get; set; }
        public string MetodoPago { get; set; } = "Efectivo";
        public decimal MontoPagado { get; set; }
    }

    public class EntrenadorDto
    {
        public int Id { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
    }

    public class RegistroSocioResult
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public string FolioQr { get; set; } = string.Empty;
    }

    public class SocioRecepcionDto
    {
        public int IdUsuario { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string? TipoMembresia { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public int? IdEntrenadorPersonal { get; set; }
        public string? NombreEntrenador { get; set; }
        public string EstadoMembresia { get; set; } = string.Empty;
        public int? DiasRestantes { get; set; }
    }

    public class AsignarEntrenadorRecepcionDto
    {
        public int? IdEntrenadorPersonal { get; set; }
    }

    public class RenovarMembresiaDto
    {
        public string TipoMembresia { get; set; } = "Normal";
        public string MetodoPago { get; set; } = "Efectivo";
        public decimal MontoPagado { get; set; }
    }

    // ==========================================
    // CONTROLADOR DE RECEPCIÓN
    // ==========================================


    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrador,Recepcionista")]
    public class RecepcionController : ControllerBase

    {
        private readonly EmailService _emailService;
        private readonly ApplicationDbContext _context;

        public RecepcionController(
            EmailService emailService,
            ApplicationDbContext context)
        {
            _emailService = emailService;
            _context = context;
        }

        // ==========================================
        // 1. OBTENER ENTRENADORES ACTIVOS
        // ==========================================

        [HttpGet("entrenadores")]
        public async Task<IActionResult> ObtenerEntrenadores()
        {
            var entrenadores = await _context.Usuarios
                .AsNoTracking()
                .Where(u =>
                    u.Rol == "Entrenador" &&
                    u.Estado == "Activo")
                .OrderBy(u => u.Nombre)
                .Select(u => new EntrenadorDto
                {
                    Id = u.IdUsuario,
                    NombreCompleto = u.Nombre
                })
                .ToListAsync();

            return Ok(entrenadores);
        }

        // ==========================================
        // 2. CONSULTAR SOCIOS Y VIGENCIAS
        // ==========================================

        [HttpGet("socios")]
        public async Task<IActionResult> ObtenerSocios()
        {
            var usuarios = await _context.Usuarios
                .AsNoTracking()
                .Where(u => u.Rol == "Socio")
                .OrderBy(u => u.Nombre)
                .ToListAsync();

            var entrenadores = await _context.Usuarios
                .AsNoTracking()
                .Where(u => u.Rol == "Entrenador")
                .Select(u => new
                {
                    u.IdUsuario,
                    u.Nombre
                })
                .ToDictionaryAsync(
                    u => u.IdUsuario,
                    u => u.Nombre);

            DateTime hoy = DateTime.Today;

            var resultado = usuarios.Select(u =>
            {
                int? diasRestantes =
                    u.FechaVencimiento.HasValue
                        ? (u.FechaVencimiento.Value.Date - hoy).Days
                        : null;

                string estadoMembresia;

                if (!string.Equals(
                    u.Estado,
                    "Activo",
                    StringComparison.OrdinalIgnoreCase))
                {
                    estadoMembresia = "Inactivo";
                }
                else if (!diasRestantes.HasValue)
                {
                    estadoMembresia = "Sin vigencia";
                }
                else if (diasRestantes.Value < 0)
                {
                    estadoMembresia = "Vencida";
                }
                else if (diasRestantes.Value <= 7)
                {
                    estadoMembresia = "Por vencer";
                }
                else
                {
                    estadoMembresia = "Vigente";
                }

                string? nombreEntrenador = null;

                if (u.IdEntrenadorPersonal.HasValue)
                {
                    entrenadores.TryGetValue(
                        u.IdEntrenadorPersonal.Value,
                        out nombreEntrenador);
                }

                return new SocioRecepcionDto
                {
                    IdUsuario = u.IdUsuario,
                    Nombre = u.Nombre,
                    Correo = u.Correo,
                    Telefono = u.Telefono,
                    TipoMembresia = u.TipoMembresia,
                    FechaVencimiento = u.FechaVencimiento,
                    IdEntrenadorPersonal =
                        u.IdEntrenadorPersonal,
                    NombreEntrenador = nombreEntrenador,
                    EstadoMembresia = estadoMembresia,
                    DiasRestantes = diasRestantes
                };

            }).ToList();

            return Ok(resultado);
        }

        // ==========================================
        // 3. RESUMEN DE MEMBRESÍAS
        // ==========================================

        [HttpGet("resumen-membresias")]
        public async Task<IActionResult> ObtenerResumenMembresias()
        {
            var socios = await _context.Usuarios
                .AsNoTracking()
                .Where(u => u.Rol == "Socio")
                .Select(u => new
                {
                    u.Estado,
                    u.FechaVencimiento
                })
                .ToListAsync();

            DateTime hoy = DateTime.Today;
            DateTime limite = hoy.AddDays(7);

            int totalSocios = socios.Count;

            int membresiasVigentes = socios.Count(s =>
                s.Estado == "Activo" &&
                s.FechaVencimiento.HasValue &&
                s.FechaVencimiento.Value.Date >= hoy);

            int proximasAVencer = socios.Count(s =>
                s.Estado == "Activo" &&
                s.FechaVencimiento.HasValue &&
                s.FechaVencimiento.Value.Date >= hoy &&
                s.FechaVencimiento.Value.Date <= limite);

            int membresiasVencidas = socios.Count(s =>
                s.FechaVencimiento.HasValue &&
                s.FechaVencimiento.Value.Date < hoy);

            return Ok(new
            {
                totalSocios,
                membresiasVigentes,
                proximasAVencer,
                membresiasVencidas
            });
        }

        // ==========================================
        // 4. ASIGNAR O CAMBIAR ENTRENADOR
        // ==========================================

        [HttpPut("socios/{idUsuario:int}/entrenador")]
        public async Task<IActionResult> AsignarEntrenador(
            int idUsuario,
            [FromBody] AsignarEntrenadorRecepcionDto dto)
        {
            var socio = await _context.Usuarios
                .FirstOrDefaultAsync(u =>
                    u.IdUsuario == idUsuario &&
                    u.Rol == "Socio");

            if (socio == null)
            {
                return NotFound(new
                {
                    exito = false,
                    mensaje = "No se encontró al socio."
                });
            }

            if (dto.IdEntrenadorPersonal.HasValue)
            {
                bool entrenadorValido =
                    await _context.Usuarios.AnyAsync(u =>
                        u.IdUsuario ==
                            dto.IdEntrenadorPersonal.Value &&
                        u.Rol == "Entrenador" &&
                        u.Estado == "Activo");

                if (!entrenadorValido)
                {
                    return BadRequest(new
                    {
                        exito = false,
                        mensaje =
                            "El entrenador no existe o está inactivo."
                    });
                }
            }

            socio.IdEntrenadorPersonal =
                dto.IdEntrenadorPersonal;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                exito = true,
                mensaje = dto.IdEntrenadorPersonal.HasValue
                    ? "Entrenador asignado correctamente."
                    : "Se retiró la asignación del entrenador."
            });
        }




        // ==========================================
        // 5. REGISTRAR SOCIO
        // ==========================================


        [HttpPost("registrar-socio")]
        public async Task<IActionResult> RegistrarSocio(
            [FromBody] RegistroSocioDto dto)
        {
            // VALIDACIONES
            if (dto == null ||
                string.IsNullOrWhiteSpace(dto.NombreCompleto) ||
                string.IsNullOrWhiteSpace(dto.Correo))
            {
                return BadRequest(new RegistroSocioResult
                {
                    Exito = false,
                    Mensaje = "El nombre y el correo son obligatorios."
                });
            }

            if (dto.MontoPagado < 0)
            {
                return BadRequest(new RegistroSocioResult
                {
                    Exito = false,
                    Mensaje = "El monto no puede ser negativo."
                });
            }

            string nombre = dto.NombreCompleto.Trim();
            string correo = dto.Correo.Trim().ToLowerInvariant();

            string tipoMembresia =
                dto.TipoMembresia?.Contains(
                    "VIP",
                    StringComparison.OrdinalIgnoreCase) == true
                    ? "VIP"
                    : "Normal";

            string metodoPago = dto.MetodoPago?.Trim() ?? "Efectivo";

            string[] metodosPermitidos =
            {
        "Efectivo",
        "Tarjeta",
        "Transferencia"
    };

            if (!metodosPermitidos.Contains(
                metodoPago,
                StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(new RegistroSocioResult
                {
                    Exito = false,
                    Mensaje = "Método de pago no válido."
                });
            }

            if (await _context.Usuarios.AnyAsync(
                u => u.Correo.ToLower() == correo))
            {
                return BadRequest(new RegistroSocioResult
                {
                    Exito = false,
                    Mensaje = "El correo ya se encuentra registrado."
                });
            }

            if (dto.IdEntrenadorPersonal.HasValue)
            {
                bool entrenadorValido =
                    await _context.Usuarios.AnyAsync(u =>
                        u.IdUsuario == dto.IdEntrenadorPersonal.Value &&
                        u.Rol == "Entrenador" &&
                        u.Estado == "Activo");

                if (!entrenadorValido)
                {
                    return BadRequest(new RegistroSocioResult
                    {
                        Exito = false,
                        Mensaje = "El entrenador seleccionado no es válido."
                    });
                }
            }

            // DATOS DEL NUEVO SOCIO
            DateTime ahora = DateTime.Now;
            DateTime vencimiento = ahora.AddMonths(1);

            string claveTemporal =
                RandomNumberGenerator.GetInt32(
                    100000, 1000000).ToString();

            string codigoQr =
                "TG-" +
                Convert.ToHexString(
                    RandomNumberGenerator.GetBytes(16));

            var nuevoSocio = new Usuario
            {
                Nombre = nombre,
                Correo = correo,
                Rol = "Socio",
                Estado = "Activo",
                Telefono = dto.Telefono?.Trim(),
                TipoMembresia = tipoMembresia,
                IdEntrenadorPersonal = dto.IdEntrenadorPersonal,
                MetodoPago = metodoPago,
                MontoPagado = dto.MontoPagado,
                FechaCreacion = ahora,
                FechaVencimiento = vencimiento
            };

            var passwordHasher = new PasswordHasher<Usuario>();

            nuevoSocio.ContrasenaHash =
                passwordHasher.HashPassword(
                    nuevoSocio, claveTemporal);

            // TRANSACCIÓN DE BASE DE DATOS
            try
            {
                await using var transaccion =
                    await _context.Database.BeginTransactionAsync(
                        System.Data.IsolationLevel.Serializable);

                // 1. USUARIO
                _context.Usuarios.Add(nuevoSocio);
                await _context.SaveChangesAsync();

                // 2. FICHA DEL SOCIO
                var fichaSocio = new Socio
                {
                    IdUsuario = nuevoSocio.IdUsuario,
                    FechaRegistro = ahora,
                    Telefono = dto.Telefono?.Trim(),
                    QrAcceso = codigoQr,
                    SaldoSesionesVIP = 0
                };

                _context.Socios.Add(fichaSocio);
                await _context.SaveChangesAsync();

                // 3. MEMBRESÍA
                var membresia = new Membresia
                {
                    IdSocio = nuevoSocio.IdUsuario,
                    Tipo = tipoMembresia,
                    FechaInicio = ahora.Date,
                    FechaFin = vencimiento.Date,
                    Precio = dto.MontoPagado,
                    Estado = "Activa"
                };

                _context.Membresias.Add(membresia);
                await _context.SaveChangesAsync();

                // 4. PAGO Y CAJA
                // No se genera un pago cuando el monto es cero.

                if (dto.MontoPagado > 0)
                {
                    // Buscar la caja que esté abierta,
                    // sin importar la fecha de apertura.
                    var caja = await _context.Cajas
                        .Where(c => c.Estado == "Abierta")
                        .OrderByDescending(c => c.Fecha)
                        .ThenByDescending(c => c.IdCaja)
                        .FirstOrDefaultAsync();

                    // No permitir pagos si no existe caja abierta.
                    if (caja == null)
                    {
                        throw new InvalidOperationException(
                            "No existe una caja abierta. " +
                            "Solicita al administrador abrir una caja.");
                    }

                    // Verificar el límite de ingresos.
                    if (caja.TotalIngresos + dto.MontoPagado
                        > 99999999.99m)
                    {
                        throw new InvalidOperationException(
                            "El pago supera el límite permitido " +
                            "para los ingresos de caja.");
                    }

                    // Registrar el pago de la membresía.
                    var pago = new Pago
                    {
                        IdMembresia = membresia.IdMembresia,
                        IdCaja = caja.IdCaja,
                        Fecha = ahora,
                        Monto = dto.MontoPagado,
                        Metodo = metodoPago,
                        Estado = "Pagado"
                    };

                    _context.Pagos.Add(pago);

                    // Actualizar ingresos de caja.
                    caja.TotalIngresos += dto.MontoPagado;

                    // Guardar los cambios.
                    await _context.SaveChangesAsync();
                }

                // CONFIRMAR TODOS LOS REGISTROS
                await transaccion.CommitAsync();
            }

            catch (InvalidOperationException ex)
            {
                return Conflict(new RegistroSocioResult
                {
                    Exito = false,
                    Mensaje = ex.Message
                });
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, new RegistroSocioResult
                {
                    Exito = false,
                    Mensaje = "No se pudo guardar el registro. " +
                              "Verifica las restricciones de SQL Server."
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new RegistroSocioResult
                {
                    Exito = false,
                    Mensaje =
                        "Ocurrió un error al registrar el socio " +
                        "y su información financiera."
                });
            }


            // CORREO DESPUÉS DE GUARDAR LA TRANSACCIÓN
            bool correoEnviado = false;

            try
            {
                await _emailService.EnviarCorreoBienvenidaAsync(
                    correo,
                    nombre,
                    claveTemporal,
                    tipoMembresia);

                correoEnviado = true;
            }
            catch (Exception)
            {
                // No revertir el registro si falla el correo.
            }

            return Ok(new RegistroSocioResult
            {
                Exito = true,
                Mensaje = correoEnviado
                    ? "Socio, membresía y pago registrados. " +
                      "Credenciales enviadas al correo."
                    : "Registro completado, pero no se pudo " +
                      "enviar el correo de bienvenida.",
                FolioQr = codigoQr
            });
        }

        // ==========================================
        // 6. RENOVAR MEMBRESÍA EXISTENTE
        // ==========================================
        [HttpPost("socios/{idUsuario:int}/renovar")]
        public async Task<IActionResult> RenovarMembresia(
            int idUsuario, [FromBody] RenovarMembresiaDto dto)
        {
            if (dto == null || dto.MontoPagado <= 0 || dto.MontoPagado > 99999999.99m)
                return BadRequest(new { exito = false, mensaje = "Ingresa un monto válido mayor a cero." });

            string tipo = dto.TipoMembresia?.Trim() ?? "";
            if (tipo != "Normal" && tipo != "VIP")
                return BadRequest(new { exito = false, mensaje = "La membresía debe ser Normal o VIP." });

            string metodo = dto.MetodoPago?.Trim() ?? "";
            if (!new[] { "Efectivo", "Tarjeta", "Transferencia" }
                .Contains(metodo, StringComparer.OrdinalIgnoreCase))
                return BadRequest(new { exito = false, mensaje = "Método de pago no válido." });

            try
            {
                await using var transaccion = await _context.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);

                var socio = await _context.Usuarios.FirstOrDefaultAsync(u =>
                    u.IdUsuario == idUsuario && u.Rol == "Socio");
                if (socio == null)
                    return NotFound(new { exito = false, mensaje = "Socio no encontrado." });

                if (!await _context.Socios.AnyAsync(s => s.IdUsuario == idUsuario))
                    return Conflict(new { exito = false, mensaje = "El socio no tiene ficha de acceso registrada." });

                var caja = await _context.Cajas
                    .Where(c => c.Estado == "Abierta")
                    .OrderByDescending(c => c.Fecha)
                    .ThenByDescending(c => c.IdCaja)
                    .FirstOrDefaultAsync();
                if (caja == null)
                    return Conflict(new { exito = false, mensaje = "No hay caja abierta. Solicita al administrador abrirla." });

                if (caja.TotalIngresos + dto.MontoPagado > 99999999.99m)
                    return Conflict(new { exito = false, mensaje = "El monto supera el límite de ingresos de caja." });

                var hoy = DateTime.Today;
                var inicio = socio.FechaVencimiento.HasValue && socio.FechaVencimiento.Value.Date > hoy
                    ? socio.FechaVencimiento.Value.Date : hoy;
                var vencimiento = inicio.AddMonths(1);

                var membresia = new Membresia
                {
                    IdSocio = socio.IdUsuario,
                    Tipo = tipo,
                    FechaInicio = inicio,
                    FechaFin = vencimiento,
                    Precio = dto.MontoPagado,
                    Estado = "Activa"
                };
                _context.Membresias.Add(membresia);
                await _context.SaveChangesAsync();

                _context.Pagos.Add(new Pago
                {
                    IdMembresia = membresia.IdMembresia,
                    IdCaja = caja.IdCaja,
                    Fecha = DateTime.Now,
                    Monto = dto.MontoPagado,
                    Metodo = metodo,
                    Estado = "Pagado"
                });

                caja.TotalIngresos += dto.MontoPagado;
                socio.TipoMembresia = tipo;
                socio.FechaVencimiento = vencimiento;
                socio.Estado = "Activo";
                socio.MetodoPago = metodo;
                socio.MontoPagado = dto.MontoPagado;

                await _context.SaveChangesAsync();
                await transaccion.CommitAsync();

                return Ok(new
                {
                    exito = true,
                    mensaje = "Membresía renovada y pago registrado correctamente.",
                    idUsuario,
                    tipoMembresia = tipo,
                    fechaVencimiento = vencimiento,
                    montoPagado = dto.MontoPagado
                });
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, new { exito = false, mensaje = "No se pudo guardar la renovación en SQL Server." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { exito = false, mensaje = "Error al procesar la renovación. Verifica los movimientos antes de reintentar." });
            }
        }

    }
}
