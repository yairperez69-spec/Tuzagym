
using System;
using System.Text.Json.Serialization;

namespace Tuzagym.Models
{
    public class LoginRequest
    {
        public string Correo { get; set; } = string.Empty;
        public string Contrasena { get; set; } = string.Empty;
    }

    public class UsuarioDto
    {
        [JsonPropertyName("idUsuario")]
        public int IdUsuario { get; set; }

        [JsonPropertyName("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [JsonPropertyName("correo")]
        public string Correo { get; set; } = string.Empty;

        [JsonPropertyName("rol")]
        public string Rol { get; set; } = string.Empty;

        [JsonPropertyName("estado")]
        public string Estado { get; set; } = string.Empty;

        [JsonPropertyName("tipoMembresia")]
        public string? TipoMembresia { get; set; }

        [JsonPropertyName("fechaVencimiento")]
        public DateTime? FechaVencimiento { get; set; }

        [JsonPropertyName("codigoQR")]
        public string? CodigoQR { get; set; }
    }

    public class LoginResult
    {
        [JsonPropertyName("exito")]
        public bool Exito { get; set; }

        [JsonPropertyName("mensaje")]
        public string Mensaje { get; set; } = string.Empty;

        [JsonPropertyName("usuario")]
        public UsuarioDto? Usuario { get; set; }

        // Token JWT generado por la API.
        [JsonPropertyName("token")]
        public string? Token { get; set; }

        // Fecha de vencimiento del token.
        [JsonPropertyName("tokenExpiracion")]
        public DateTime? TokenExpiracion { get; set; }


        // Propiedades de conveniencia.
        [JsonIgnore]
        public int IdUsuario => Usuario?.IdUsuario ?? 0;

        [JsonIgnore]
        public string Rol => Usuario?.Rol ?? string.Empty;

        [JsonIgnore]
        public string Nombre => Usuario?.Nombre ?? string.Empty;

        [JsonIgnore]
        public string? TipoMembresia => Usuario?.TipoMembresia;

        [JsonIgnore]
        public DateTime? FechaVencimiento =>
            Usuario?.FechaVencimiento;

        [JsonIgnore]
        public string? CodigoQR => Usuario?.CodigoQR;

    }
}
