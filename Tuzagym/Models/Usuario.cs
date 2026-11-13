using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tuzagym.API.Models // <-- Debe decir Tuzagym.API.Models
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
        public string Estado { get; set; } = "Activo"; // <-- Debe ser string, no bool

        [Column("rol")]
        public string Rol { get; set; } = string.Empty;

        [Column("fechaCreacion")]
        public DateTime? FechaCreacion { get; set; }
    }
}
