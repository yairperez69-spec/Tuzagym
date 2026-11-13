
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Tuzagym.API.Data;

namespace Tuzagym.API.Services
{
    public sealed class RecordatoriosMembresiaService
        : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<RecordatoriosMembresiaService> _logger;

        // Control temporal: no se guarda en SQL Server.
        private DateOnly? _ultimaRevision;

        public RecordatoriosMembresiaService(
            IServiceScopeFactory scopeFactory,
            IConfiguration config,
            ILogger<RecordatoriosMembresiaService> logger)
        {
            _scopeFactory = scopeFactory;
            _config = config;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            TimeZoneInfo zonaHoraria = ObtenerZonaHoraria();

            _logger.LogInformation(
                "Servicio de recordatorios TuzaGym iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    bool habilitado = _config.GetValue<bool>(
                        "RecordatoriosMembresia:Habilitado");

                    DateTimeOffset ahora =
                        TimeZoneInfo.ConvertTime(
                            DateTimeOffset.UtcNow,
                            zonaHoraria);

                    DateOnly fechaHoy =
                        DateOnly.FromDateTime(ahora.Date);

                    TimeSpan horaProgramada =
                        new TimeSpan(9, 0, 0);

                    if (habilitado &&
                        ahora.TimeOfDay >= horaProgramada &&
                        _ultimaRevision != fechaHoy)
                    {
                        // Evita repetir la revisión mientras
                        // esta instancia de la API siga activa.
                        _ultimaRevision = fechaHoy;

                        _logger.LogInformation(
                            "Iniciando revisión de membresías.");

                        await RevisarMembresiasAsync(
                            ahora.Date,
                            stoppingToken);
                    }

                    await Task.Delay(
                        TimeSpan.FromMinutes(1),
                        stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error al revisar membresías.");

                    try
                    {
                        await Task.Delay(
                            TimeSpan.FromMinutes(1),
                            stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }

        private async Task RevisarMembresiasAsync(
            DateTime fechaHoy,
            CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var emailService = scope.ServiceProvider
                .GetRequiredService<EmailService>();

            var socios = await db.Usuarios
                .AsNoTracking()
                .Where(u =>
                    u.Rol == "Socio" &&
                    u.Estado == "Activo" &&
                    u.FechaVencimiento != null)
                .Select(u => new
                {
                    u.IdUsuario,
                    u.Nombre,
                    u.Correo,
                    u.TipoMembresia,
                    u.FechaVencimiento
                })
                .ToListAsync(cancellationToken);

            int enviados = 0;
            int fallidos = 0;

            foreach (var socio in socios)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(socio.Correo) ||
                    !socio.FechaVencimiento.HasValue)
                {
                    continue;
                }

                DateTime vencimiento =
                    socio.FechaVencimiento.Value.Date;

                int diasRestantes =
                    (vencimiento - fechaHoy.Date).Days;

                // Avisos únicamente en los días establecidos.
                if (diasRestantes != 7 &&
                    diasRestantes != 3 &&
                    diasRestantes != 0 &&
                    diasRestantes != -1)
                {
                    continue;
                }

                try
                {
                    bool resultado;

                    if (diasRestantes >= 0)
                    {
                        resultado = await emailService
                            .EnviarRecordatorioVencimientoAsync(
                                socio.Correo,
                                socio.Nombre,
                                socio.TipoMembresia ?? "Normal",
                                vencimiento,
                                diasRestantes);
                    }
                    else
                    {
                        resultado = await emailService
                            .EnviarMembresiaVencidaAsync(
                                socio.Correo,
                                socio.Nombre,
                                socio.TipoMembresia ?? "Normal",
                                vencimiento);
                    }

                    if (resultado)
                    {
                        enviados++;

                        _logger.LogInformation(
                            "Aviso enviado al socio ID {IdUsuario}.",
                            socio.IdUsuario);
                    }
                    else
                    {
                        fallidos++;

                        _logger.LogWarning(
                            "Falló el aviso al socio ID {IdUsuario}.",
                            socio.IdUsuario);
                    }
                }
                catch (Exception ex)
                {
                    fallidos++;

                    _logger.LogError(
                        ex,
                        "Error al enviar aviso al socio ID {IdUsuario}.",
                        socio.IdUsuario);
                }
            }

            _logger.LogInformation(
                "Revisión terminada. Enviados: {Enviados}. Fallidos: {Fallidos}.",
                enviados,
                fallidos);
        }

        private static TimeZoneInfo ObtenerZonaHoraria()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(
                    "America/Mexico_City");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById(
                    "Central Standard Time (Mexico)");
            }
        }
    }
}
