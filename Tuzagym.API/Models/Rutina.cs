
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tuzagym.API.Models
{
    [Table("Rutina", Schema = "dbo")]
    public class Rutina
    {
        [Key]
        [Column("idRutina")]
        public int IdRutina { get; set; }

        [Required]
        [Column("idEntrenador")]
        public int IdEntrenador { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("nombre")]
        public string Nombre { get; set; } = "";

        [Required]
        [MaxLength(150)]
        [Column("objetivo")]
        public string Objetivo { get; set; } = "";

        [Required]
        [MaxLength(100)]
        [Column("nivel")]
        public string Nivel { get; set; } = "";
    }
}
