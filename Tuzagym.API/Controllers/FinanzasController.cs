
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tuzagym.API.Data;
using Tuzagym.API.DTOs;

namespace Tuzagym.API.Controllers
{
    [ApiController]
    [Route("api/finanzas")]
    [Authorize]
    public class FinanzasController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public FinanzasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================
        // ABRIR NUEVA CAJA
        // SOLO ADMINISTRADOR
        // =========================================

        [HttpPost("caja/abrir")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> AbrirCaja()
        {
            await using var transaccion =
                await _context.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);

            try
            {
                // Verificar si ya existe una caja abierta
                bool existeCajaAbierta =
                    await _context.Cajas.AnyAsync(
                        c => c.Estado == "Abierta");

                if (existeCajaAbierta)
                {
                    await transaccion.RollbackAsync();

                    return Conflict(new
                    {
                        exito = false,
                        mensaje = "Ya existe una caja abierta."
                    });
                }

                // Crear una nueva caja
                var nuevaCaja = new Caja
                {
                    Fecha = DateTime.Today,
                    TotalIngresos = 0,
                    TotalEgresos = 0,
                    Estado = "Abierta"
                };

                _context.Cajas.Add(nuevaCaja);

                await _context.SaveChangesAsync();
                await transaccion.CommitAsync();

                return Ok(new
                {
                    exito = true,
                    mensaje = "Caja abierta correctamente.",
                    idCaja = nuevaCaja.IdCaja,
                    fecha = nuevaCaja.Fecha,
                    totalIngresos = nuevaCaja.TotalIngresos,
                    totalEgresos = nuevaCaja.TotalEgresos,
                    estado = nuevaCaja.Estado
                });
            }
            catch (Exception)
            {
                await transaccion.RollbackAsync();

                return StatusCode(500, new
                {
                    exito = false,
                    mensaje = "Ocurrió un error al abrir la caja."
                });
            }
        }

        // =========================================
        // CONSULTAR ARQUEO DE CAJA
        // SOLO ADMINISTRADOR
        // =========================================

        [HttpGet("caja/arqueo")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ObtenerArqueoCaja()
        {

            var caja = await _context.Cajas
                .AsNoTracking()
                .OrderByDescending(c => c.Fecha)
                .ThenByDescending(c => c.IdCaja)
                .FirstOrDefaultAsync();


            if (caja == null)
            {
                return NotFound(new
                {
                    exito = false,
                    mensaje = "No existen cajas registradas."
                });
            }

            decimal saldo =
                caja.TotalIngresos - caja.TotalEgresos;

            return Ok(new
            {
                exito = true,
                idCaja = caja.IdCaja,
                fecha = caja.Fecha,
                estado = caja.Estado,
                totalIngresos = caja.TotalIngresos,
                totalEgresos = caja.TotalEgresos,
                saldoActual = saldo
            });
        }

        // =========================================
        // RESUMEN FINANCIERO
        // SOLO ADMINISTRADOR
        // =========================================

        [HttpGet("resumen")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ObtenerResumen()
        {
            var hoy = DateTime.Today;
            var inicioMes = new DateTime(
                hoy.Year, hoy.Month, 1);

            var inicioMesSiguiente = inicioMes.AddMonths(1);

            var pagosMes = await _context.Pagos
                .AsNoTracking()
                .Where(p =>
                    p.Fecha >= inicioMes &&
                    p.Fecha < inicioMesSiguiente &&
                    p.Estado == "Pagado")
                .ToListAsync();


            var gastosMesRegistrados = await _context.Gastos
                .AsNoTracking()
                .Where(g =>
                    g.Fecha >= inicioMes &&
                    g.Fecha < inicioMesSiguiente)
                .ToListAsync();

            decimal ventasMes = pagosMes.Sum(p => p.Monto);

            decimal gastosMes = gastosMesRegistrados
                .Sum(g => g.Monto);

            decimal gananciasMes = ventasMes - gastosMes;


            var ultimaCaja = await _context.Cajas
                .AsNoTracking()
                .OrderByDescending(c => c.Fecha)
                .ThenByDescending(c => c.IdCaja)
                .FirstOrDefaultAsync();

            decimal saldoCaja = ultimaCaja == null
                ? 0
                : ultimaCaja.TotalIngresos -
                  ultimaCaja.TotalEgresos;

            return Ok(new
            {
                ventasMes,
                gastosMes,
                gananciasMes,
                saldoCaja,
                estadoCaja = ultimaCaja?.Estado ?? "Sin registros",
                fechaConsulta = DateTime.Now,
                hayDatosCaja = ultimaCaja != null
            });
        }

        // =========================================
        // ARQUEO Y CIERRE DE CAJA
        // SOLO ADMINISTRADOR
        // =========================================

        [HttpPost("caja/cerrar")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> CerrarCaja()
        {
            await using var transaccion =
                await _context.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);

            try
            {
                // Buscar la última caja abierta.
                var caja = await _context.Cajas
                    .Where(c => c.Estado == "Abierta")
                    .OrderByDescending(c => c.Fecha)
                    .ThenByDescending(c => c.IdCaja)
                    .FirstOrDefaultAsync();

                if (caja == null)
                {
                    await transaccion.RollbackAsync();

                    return Conflict(new
                    {
                        exito = false,
                        mensaje = "No existe una caja abierta para cerrar."
                    });
                }

                // Obtener los totales registrados.
                decimal ingresos = caja.TotalIngresos;
                decimal egresos = caja.TotalEgresos;
                decimal saldo = ingresos - egresos;

                // Cambiar el estado de la caja.
                caja.Estado = "Cerrada";

                await _context.SaveChangesAsync();
                await transaccion.CommitAsync();

                return Ok(new
                {
                    exito = true,
                    mensaje = "Caja cerrada correctamente.",
                    idCaja = caja.IdCaja,
                    fecha = caja.Fecha,
                    totalIngresos = ingresos,
                    totalEgresos = egresos,
                    saldoFinal = saldo,
                    estado = caja.Estado
                });
            }
            catch (Exception)
            {
                await transaccion.RollbackAsync();

                return StatusCode(500, new
                {
                    exito = false,
                    mensaje = "Ocurrió un error al cerrar la caja."
                });
            }
        }

        // =========================================
        // REGISTRAR GASTOS
        // ADMINISTRADOR Y RECEPCIONISTA
        // =========================================

        [HttpPost("gastos")]
        [Authorize(Roles = "Administrador,Recepcionista")]
        public async Task<IActionResult> RegistrarGasto(
            [FromBody] RegistrarGastoDto solicitud)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            string concepto =
                solicitud.Concepto?.Trim() ?? string.Empty;

            if (concepto.Length < 3 ||
                concepto.Length > 200)
            {
                return BadRequest(new
                {
                    exito = false,
                    mensaje = "El concepto debe tener entre 3 y 200 caracteres."
                });
            }

            if (solicitud.Monto <= 0 ||
                solicitud.Monto > 99999999.99m)
            {
                return BadRequest(new
                {
                    exito = false,
                    mensaje = "El monto debe ser mayor que cero."
                });
            }

            // Identificar al usuario autenticado
            string? responsable =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(responsable))
            {
                return Unauthorized(new
                {
                    exito = false,
                    mensaje = "No se pudo identificar al usuario autenticado."
                });
            }

            // Evitar inconsistencias al actualizar caja
            await using var transaccion =
                await _context.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);

            try
            {
                // Buscar la última caja abierta
                var caja = await _context.Cajas
                    .Where(c => c.Estado == "Abierta")
                    .OrderByDescending(c => c.Fecha)
                    .ThenByDescending(c => c.IdCaja)
                    .FirstOrDefaultAsync();

                if (caja == null)
                {
                    await transaccion.RollbackAsync();

                    return Conflict(new
                    {
                        exito = false,
                        mensaje = "No existe una caja abierta. No se puede registrar el gasto."
                    });
                }

                // Verificar límite del decimal(10,2)
                if (caja.TotalEgresos + solicitud.Monto
                    > 99999999.99m)
                {
                    await transaccion.RollbackAsync();

                    return BadRequest(new
                    {
                        exito = false,
                        mensaje = "El gasto supera el límite permitido para la caja."
                    });
                }

                // Crear registro del gasto
                var gasto = new Gasto
                {
                    IdCaja = caja.IdCaja,
                    Concepto = concepto,
                    Monto = solicitud.Monto,
                    Fecha = DateTime.Now,
                    Responsable = responsable
                };

                _context.Gastos.Add(gasto);

                // Actualizar egresos de caja
                caja.TotalEgresos += solicitud.Monto;

                // Guardar ambos cambios
                await _context.SaveChangesAsync();

                await transaccion.CommitAsync();

                return Ok(new
                {
                    exito = true,
                    mensaje = "Gasto registrado correctamente.",
                    idGasto = gasto.IdGasto,
                    idCaja = caja.IdCaja,
                    concepto = gasto.Concepto,
                    monto = gasto.Monto,
                    responsable = gasto.Responsable,
                    totalEgresos = caja.TotalEgresos
                });
            }
            catch (Exception)
            {
                await transaccion.RollbackAsync();

                return StatusCode(500, new
                {
                    exito = false,
                    mensaje = "Ocurrió un error al registrar el gasto."
                });
            }
        }

        // =========================================
        // CONSULTAR GASTOS
        // ADMINISTRADOR Y RECEPCIONISTA
        // =========================================

        [HttpGet("gastos")]
        [Authorize(Roles = "Administrador,Recepcionista")]
        public async Task<IActionResult> ObtenerGastos()
        {
            var gastos = await _context.Gastos
                .AsNoTracking()
                .OrderByDescending(g => g.Fecha)
                .ThenByDescending(g => g.IdGasto)
                .Take(100)
                .Select(g => new
                {
                    g.IdGasto,
                    g.IdCaja,
                    g.Concepto,
                    g.Monto,
                    g.Fecha,
                    g.Responsable
                })
                .ToListAsync();

            return Ok(new
            {
                exito = true,
                total = gastos.Count,
                gastos
            });
        }

        // =========================================
        // CONSULTAR ESTADO DE CAJA
        // ADMINISTRADOR Y RECEPCIONISTA
        // =========================================

        [HttpGet("caja/estado")]
        [Authorize(Roles = "Administrador,Recepcionista")]
        public async Task<IActionResult> ObtenerEstadoCaja()
        {
            var caja = await _context.Cajas
                .AsNoTracking()
                .OrderByDescending(c => c.Fecha)
                .ThenByDescending(c => c.IdCaja)
                .FirstOrDefaultAsync();

            if (caja == null)
            {
                return Ok(new
                {
                    hayCaja = false,
                    estado = "Sin registros"
                });
            }

            return Ok(new
            {
                hayCaja = true,
                idCaja = caja.IdCaja,
                fecha = caja.Fecha,
                estado = caja.Estado
            });
        }
    }
}
