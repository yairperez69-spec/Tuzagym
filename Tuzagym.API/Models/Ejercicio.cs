
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tuzagym.API.Models
{
    [Table("Ejercicio", Schema = "dbo")]
    public class Ejercicio
    {
        [Key]
        [Column("idEjercicio")]
        public int IdEjercicio { get; set; }

        [Column("idRutina")]
        public int IdRutina { get; set; }

        [Required]
        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Column("series")]
        public int Series { get; set; }

        [Column("repeticiones")]
        public int Repeticiones { get; set; }

        [Column("descansoSeg")]
        public int DescansoSeg { get; set; }
    }
}
