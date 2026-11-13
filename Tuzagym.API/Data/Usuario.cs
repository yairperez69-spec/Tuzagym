using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tuzagym.API.Data
{
    [Table("Usuarios")]
    public class Usuario
    {
        [Key]
        [Column("idUsuario")]
        public int IdUsuario { get; set; }

        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Column("correo")]
        public string Correo { get; set; } = string.Empty;

        [Column("contrasenaHash")]
        public string ContrasenaHash { get; set; } = string.Empty;

        [Column("estado")]
        public string Estado { get; set; } = "Activo";

        [Column("rol")]
        public string Rol { get; set; } = "Socio";

        [Column("telefono")]
        public string? Telefono { get; set; }

        [Column("tipoMembresia")]
        public string? TipoMembresia { get; set; } = "Normal";

        [Column("idEntrenadorPersonal")]
        public int? IdEntrenadorPersonal { get; set; }

        [Column("metodoPago")]
        public string? MetodoPago { get; set; } = "Efectivo";

        [Column("montoPagado")]
        public decimal? MontoPagado { get; set; } = 0; // <-- Cambiado a decimal?

        [Column("fechaCreacion")]
        public DateTime? FechaCreacion { get; set; } = DateTime.Now;

        [Column("fechaVencimiento")]
        public DateTime? FechaVencimiento { get; set; } = DateTime.Now.AddMonths(1);
    }
}