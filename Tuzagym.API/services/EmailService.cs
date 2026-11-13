
using System;
using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Tuzagym.API.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IConfiguration config,
            ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        // CORREO ORIGINAL: CODIGO DE VERIFICACION
        public async Task<bool> EnviarCorreoBienvenidaAsync(
            string correoDestino,
            string nombreSocio,
            string codigoVerificacion,
            string tipoMembresia)
        {
            string nombre = HtmlEncoder.Default.Encode(
                nombreSocio ?? "Socio");

            string tipo = HtmlEncoder.Default.Encode(
                tipoMembresia ?? "Normal");

            string codigo = HtmlEncoder.Default.Encode(
                codigoVerificacion ?? "");

            string detalle =
                (tipoMembresia ?? "").Contains(
                    "VIP",
                    StringComparison.OrdinalIgnoreCase)
                ? "Tu membresía VIP incluye entrenador personal asignado y seguimiento de progreso."
                : "Tu membresía Normal te da acceso a las instalaciones con tu código QR.";

            string cuerpo = $@"
                <h2 style='color:#00E676'>
                    ¡Bienvenido a TuzaGym, {nombre}!
                </h2>
                <p>Tu registro en recepción se ha realizado exitosamente.</p>
                <p><strong>Tipo de membresía:</strong> {tipo}</p>
                <p>{detalle}</p>
                <hr style='border:0;border-top:1px solid #334155'>
                <p>Tu código de verificación de cuenta es:</p>
                <p style='font-size:28px;color:#38BDF8;letter-spacing:4px'>
                    <strong>{codigo}</strong>
                </p>";

            return await EnviarAsync(
                correoDestino,
                "¡Bienvenido a TuzaGym! - Código de Verificación",
                cuerpo);
        }

        public Task<bool> EnviarCodigoRecuperacionAsync(
            string correoDestino,
            string nombreUsuario,
            string codigo)
        {
            string nombre = System.Text.Encodings.Web
                .HtmlEncoder.Default.Encode(nombreUsuario);

            string codigoSeguro = System.Text.Encodings.Web
                .HtmlEncoder.Default.Encode(codigo);

            string cuerpo = $@"
        <h2 style='color:#00E676'>
            Recuperación de contraseña - TuzaGym
        </h2>
        <p>Hola, {nombre}.</p>
        <p>Recibimos una solicitud para cambiar tu contraseña.</p>
        <p>Tu código de recuperación es:</p>
        <h1 style='color:#38BDF8;letter-spacing:5px'>
            {codigoSeguro}
        </h1>
        <p>Este código vence en 10 minutos.</p>
        <p>
            Si no solicitaste este cambio,
            puedes ignorar este mensaje.
        </p>";

            return EnviarAsync(
                correoDestino,
                "TuzaGym | Recuperación de contraseña",
                cuerpo);
        }

        // RECORDATORIOS DE 7, 3 Y 0 DIAS
        public Task<bool> EnviarRecordatorioVencimientoAsync(
            string correoDestino,
            string nombreSocio,
            string tipoMembresia,
            DateTime fechaVencimiento,
            int diasRestantes)
        {
            if (diasRestantes < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(diasRestantes));

            string nombre = HtmlEncoder.Default.Encode(
                nombreSocio ?? "Socio");

            string tipo = HtmlEncoder.Default.Encode(
                tipoMembresia ?? "No especificada");

            string fecha = FormatearFecha(fechaVencimiento);

            string plazo = diasRestantes == 0
                ? "vence hoy"
                : diasRestantes == 1
                    ? "vence mañana"
                    : $"vence en {diasRestantes} días";

            string cuerpo = $@"
                <h2 style='color:#00E676'>
                    TuzaGym - Recordatorio de membresía
                </h2>
                <p>Hola, {nombre}.</p>
                <p>
                    Te recordamos que tu membresía
                    <strong>{tipo}</strong> {plazo}.
                </p>
                <p>
                    <strong>Fecha de vencimiento:</strong>
                    {fecha}
                </p>
                <p>
                    Para continuar entrenando sin interrupciones,
                    puedes renovar en recepción.
                </p>
                <p>Si ya renovaste, ignora este mensaje.</p>";

            string asunto = diasRestantes == 0
                ? "TuzaGym | Tu membresía vence hoy"
                : $"TuzaGym | Tu membresía vence en {diasRestantes} días";

            return EnviarAsync(correoDestino, asunto, cuerpo);
        }

        // MEMBRESIA VENCIDA
        public Task<bool> EnviarMembresiaVencidaAsync(
            string correoDestino,
            string nombreSocio,
            string tipoMembresia,
            DateTime fechaVencimiento)
        {
            string nombre = HtmlEncoder.Default.Encode(
                nombreSocio ?? "Socio");

            string tipo = HtmlEncoder.Default.Encode(
                tipoMembresia ?? "No especificada");

            string cuerpo = $@"
                <h2 style='color:#00E676'>
                    TuzaGym - Renovación pendiente
                </h2>
                <p>Hola, {nombre}.</p>
                <p>
                    Tu membresía <strong>{tipo}</strong>
                    venció el {FormatearFecha(fechaVencimiento)}.
                </p>
                <p>
                    Te invitamos a acudir a recepción
                    para renovarla y continuar con tus entrenamientos.
                </p>
                <p>Si ya renovaste, ignora este mensaje.</p>";

            return EnviarAsync(
                correoDestino,
                "TuzaGym | Tu membresía está vencida",
                cuerpo);
        }

        // CONFIRMACION DE RENOVACION
        public Task<bool> EnviarConfirmacionRenovacionAsync(
            string correoDestino,
            string nombreSocio,
            string tipoMembresia,
            DateTime nuevaFechaVencimiento)
        {
            string nombre = HtmlEncoder.Default.Encode(
                nombreSocio ?? "Socio");

            string tipo = HtmlEncoder.Default.Encode(
                tipoMembresia ?? "No especificada");

            string cuerpo = $@"
                <h2 style='color:#00E676'>
                    ¡Tu membresía fue renovada!
                </h2>
                <p>Hola, {nombre}.</p>
                <p>
                    Tu membresía <strong>{tipo}</strong>
                    se renovó correctamente.
                </p>
                <p>
                    <strong>Nueva fecha de vencimiento:</strong>
                    {FormatearFecha(nuevaFechaVencimiento)}
                </p>
                <p>
                    Gracias por seguir entrenando con TuzaGym.
                </p>";

            return EnviarAsync(
                correoDestino,
                "TuzaGym | Confirmación de renovación",
                cuerpo);
        }

        private static string FormatearFecha(DateTime fecha)
        {
            return fecha.ToString(
                "dd 'de' MMMM 'de' yyyy",
                new CultureInfo("es-MX"));
        }

        // ENVIO SMTP COMPARTIDO
        private async Task<bool> EnviarAsync(
            string destino,
            string asunto,
            string contenidoHtml)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(destino))
                    throw new ArgumentException(
                        "El correo de destino está vacío.");

                var smtp = _config.GetSection("SmtpSettings");

                string servidor = smtp["Server"]
                    ?? throw new InvalidOperationException(
                        "Falta SmtpSettings:Server.");

                string usuario = smtp["Username"]
                    ?? throw new InvalidOperationException(
                        "Falta SmtpSettings:Username.");

                string clave = smtp["Password"]
                    ?? throw new InvalidOperationException(
                        "Falta SmtpSettings:Password.");

                string remitente = smtp["SenderEmail"]
                    ?? throw new InvalidOperationException(
                        "Falta SmtpSettings:SenderEmail.");

                string nombreRemitente =
                    smtp["SenderName"] ?? "TuzaGym";

                int puerto = int.Parse(
                    smtp["Port"] ?? "587",
                    CultureInfo.InvariantCulture);

                using var cliente = new SmtpClient(
                    servidor, puerto)
                {
                    Credentials = new NetworkCredential(
                        usuario, clave),
                    EnableSsl = true
                };

                using var mensaje = new MailMessage
                {
                    From = new MailAddress(
                        remitente, nombreRemitente),

                    Subject = asunto,

                    Body = $@"
                        <div style='background:#0B0F19;
                                    color:#F8FAFC;
                                    padding:28px;
                                    font-family:Arial,sans-serif;
                                    border-radius:12px;
                                    line-height:1.6'>
                            {contenidoHtml}
                            <hr style='border:0;
                                       border-top:1px solid #334155'>
                            <small style='color:#94A3B8'>
                                TuzaGym - Sistema de gestión
                            </small>
                        </div>",

                    IsBodyHtml = true
                };

                mensaje.To.Add(new MailAddress(destino));

                await cliente.SendMailAsync(mensaje);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al enviar correo de TuzaGym. Asunto: {Asunto}",
                    asunto);

                return false;
            }
        }
    }
}
 