
using System;

namespace Tuzagym.Models
{
    public class SocioRecepcionDto
    {
        public int IdUsuario { get; set; }
        public string Nombre { get; set; } = "";
        public string Correo { get; set; } = "";
        public string? Telefono { get; set; }
        public string? TipoMembresia { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public int? IdEntrenadorPersonal { get; set; }
        public string? NombreEntrenador { get; set; }
        public string EstadoMembresia { get; set; } = "";
        public int? DiasRestantes { get; set; }
    }
}
