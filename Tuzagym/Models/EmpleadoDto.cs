
using System;

namespace Tuzagym.Models
{
    public class EmpleadoDto
    {
        public int IdUsuario { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string Rol { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateTime? FechaCreacion { get; set; }
    }

    public class RegistrarEmpleadoDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string Rol { get; set; } = string.Empty;
        public string Contrasena { get; set; } = string.Empty;
        public string? Especialidad { get; set; }
        public int? LimiteClientes { get; set; }
        public string? Turno { get; set; }
    }

    public class RegistroEmpleadoResult
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public int IdUsuario { get; set; }
    }
}
