namespace Tuzagym.API.Models
{
    public class LoginRequest
    {
        public string Correo { get; set; } = string.Empty;
        public string Contrasena { get; set; } = string.Empty;
    }

    public class UsuarioDto
    {
        public int IdUsuario { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string? TipoMembresia { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public string? CodigoQR { get; set; }
    }

    public class LoginResult
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public UsuarioDto? Usuario { get; set; }
        // Token JWT generado por la API
        public string? Token { get; set; }
    }
}