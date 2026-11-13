
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Tuzagym.API.Data;
using Tuzagym.API.Models;
using Microsoft.AspNetCore.Authorization;
using Tuzagym.API.DTOs;
using Tuzagym.API.Services;

namespace Tuzagym.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly EmailService _emailService;
        private readonly RecuperacionContrasenaService _recuperacion;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        private readonly PasswordHasher<Usuario> _passwordHasher =
            new PasswordHasher<Usuario>();

        public AuthController(
     ApplicationDbContext context,
     IConfiguration configuration,
     EmailService emailService,
     RecuperacionContrasenaService recuperacion)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
            _recuperacion = recuperacion;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(
    [FromBody] LoginRequest request)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.Correo) ||
                string.IsNullOrWhiteSpace(request.Contrasena))
            {
                return BadRequest(new LoginResult
                {
                    Exito = false,
                    Mensaje = "Credenciales incompletas."
                });
            }

            string correo =
                request.Correo.Trim().ToLowerInvariant();

            // 1. BUSCAR USUARIO
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u =>
                    u.Correo.ToLower() == correo);

            if (usuario == null)
            {
                return Unauthorized(new LoginResult
                {
                    Exito = false,
                    Mensaje = "Correo o contraseña incorrectos."
                });
            }

            // 2. VERIFICAR CONTRASEÑA
            bool contrasenaCorrecta = false;
            bool requiereMigracion = false;

            string hashGuardado =
                usuario.ContrasenaHash ?? "";

            try
            {
                var resultado =
                    _passwordHasher.VerifyHashedPassword(
                        usuario,
                        hashGuardado,
                        request.Contrasena);

                if (resultado ==
                    PasswordVerificationResult.Success)
                {
                    contrasenaCorrecta = true;
                }
                else if (resultado ==
                    PasswordVerificationResult.SuccessRehashNeeded)
                {
                    contrasenaCorrecta = true;
                    requiereMigracion = true;
                }
            }
            catch (FormatException)
            {
                // Cuenta antigua.
            }
            catch (ArgumentException)
            {
                // Formato de hash no válido.
            }

            // COMPATIBILIDAD CON CUENTAS ANTIGUAS
            bool pareceHashIdentity =
                hashGuardado.StartsWith(
                    "AQAAAA",
                    StringComparison.Ordinal);

            if (!contrasenaCorrecta && !pareceHashIdentity)
            {
                if (hashGuardado == request.Contrasena)
                {
                    contrasenaCorrecta = true;
                    requiereMigracion = true;
                }
            }

            if (!contrasenaCorrecta)
            {
                return Unauthorized(new LoginResult
                {
                    Exito = false,
                    Mensaje = "Correo o contraseña incorrectos."
                });
            }

            // 3. VERIFICAR ESTADO
            if (!string.Equals(
                usuario.Estado,
                "Activo",
                StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new LoginResult
                {
                    Exito = false,
                    Mensaje = "La cuenta no está activa."
                });
            }

            // 4. MIGRAR CONTRASEÑA ANTIGUA
            if (requiereMigracion)
            {
                usuario.ContrasenaHash =
                    _passwordHasher.HashPassword(
                        usuario,
                        request.Contrasena);

                await _context.SaveChangesAsync();
            }

            // 5. CONSULTAR QR DEL SOCIO
            string? codigoQr = null;

            if (usuario.Rol.Equals(
                "Socio",
                StringComparison.OrdinalIgnoreCase))
            {
                codigoQr = await _context.Socios
                    .AsNoTracking()
                    .Where(s =>
                        s.IdUsuario == usuario.IdUsuario)
                    .Select(s => s.QrAcceso)
                    .FirstOrDefaultAsync();
            }

            // 6. GENERAR TOKEN JWT

            string token = GenerarToken(usuario);

            // 7. RESPONDER A LA APLICACIÓN
            return Ok(new LoginResult
            {
                Exito = true,
                Mensaje = "Inicio de sesión exitoso.",
                Token = token,

                Usuario = new UsuarioDto
                {
                    IdUsuario = usuario.IdUsuario,
                    Nombre = usuario.Nombre,
                    Correo = usuario.Correo,
                    Rol = usuario.Rol,
                    Estado = usuario.Estado,
                    TipoMembresia = usuario.TipoMembresia,
                    FechaVencimiento = usuario.FechaVencimiento,
                    CodigoQR = codigoQr
                }
            });
        }


        [AllowAnonymous]
        [HttpPost("solicitar-recuperacion")]
        public async Task<IActionResult> SolicitarRecuperacion(
            [FromBody] SolicitarRecuperacionDto? request)
        {
            const string respuesta =
                "Si el correo está registrado, recibirás instrucciones de recuperación.";

            if (string.IsNullOrWhiteSpace(request?.Correo))
            {
                return BadRequest(new
                {
                    mensaje = "Introduce un correo válido."
                });
            }

            string correo = request.Correo
                .Trim()
                .ToLowerInvariant();

            if (!_recuperacion.PuedeSolicitar(correo))
            {
                return Ok(new { mensaje = respuesta });
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u =>
                    u.Correo.ToLower() == correo);

            if (usuario == null ||
                !string.Equals(
                    usuario.Estado,
                    "Activo",
                    StringComparison.OrdinalIgnoreCase))
            {
                return Ok(new { mensaje = respuesta });
            }

            string? codigo =
                _recuperacion.GenerarCodigo(correo);

            if (codigo != null)
            {
                await _emailService.EnviarCodigoRecuperacionAsync(
                    usuario.Correo,
                    usuario.Nombre,
                    codigo);
            }

            return Ok(new { mensaje = respuesta });
        }



        [AllowAnonymous]
        [HttpPost("restablecer-contrasena")]
        public async Task<IActionResult> RestablecerContrasena(
            [FromBody] RestablecerContrasenaDto? request)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.Correo) ||
                string.IsNullOrWhiteSpace(request.Codigo) ||
                string.IsNullOrWhiteSpace(request.NuevaContrasena))
            {
                return BadRequest(new
                {
                    mensaje = "Todos los campos son obligatorios."
                });
            }

            string correo = request.Correo
                .Trim()
                .ToLowerInvariant();

            string codigo = request.Codigo.Trim();

            if (codigo.Length != 6 ||
                codigo.Any(c => c < '0' || c > '9'))
            {
                return BadRequest(new
                {
                    mensaje = "Código inválido o expirado."
                });
            }

            if (request.NuevaContrasena.Length < 8)
            {
                return BadRequest(new
                {
                    mensaje = "La contraseña debe tener al menos 8 caracteres."
                });
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u =>
                    u.Correo.ToLower() == correo);

            if (usuario == null ||
                !string.Equals(
                    usuario.Estado,
                    "Activo",
                    StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    mensaje = "Código inválido o expirado."
                });
            }

            if (!_recuperacion.ValidarYConsumir(
                correo,
                codigo))
            {
                return BadRequest(new
                {
                    mensaje = "Código inválido o expirado."
                });
            }

            usuario.ContrasenaHash =
                _passwordHasher.HashPassword(
                    usuario,
                    request.NuevaContrasena);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "Contraseña actualizada correctamente."
            });
        }


        private string GenerarToken(Usuario usuario)
        {
            string? clave =
                _configuration["Jwt:Key"];

            string? issuer =
                _configuration["Jwt:Issuer"];

            string? audience =
                _configuration["Jwt:Audience"];

            if (string.IsNullOrWhiteSpace(clave) ||
                Encoding.UTF8.GetByteCount(clave) < 32)
            {
                throw new InvalidOperationException(
                    "Jwt:Key no está configurada correctamente.");
            }

            if (string.IsNullOrWhiteSpace(issuer) ||
                string.IsNullOrWhiteSpace(audience))
            {
                throw new InvalidOperationException(
                    "Faltan Jwt:Issuer o Jwt:Audience.");
            }

            var claims = new[]
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    usuario.IdUsuario.ToString()),

                new Claim(
                    ClaimTypes.Name,
                    usuario.Nombre),

                new Claim(
                    ClaimTypes.Email,
                    usuario.Correo),

                new Claim(
                    ClaimTypes.Role,
                    usuario.Rol)
            };

            var llave = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(clave));

            var credenciales = new SigningCredentials(
                llave,
                SecurityAlgorithms.HmacSha256);

            var jwt = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: credenciales);

            return new JwtSecurityTokenHandler()
                .WriteToken(jwt);
        }
    }
}

