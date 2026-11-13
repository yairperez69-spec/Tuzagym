using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tuzagym.API.Data
{
    [Table("Recepcionistas")]
    public class Recepcionista
    {
        [Key]
        [Column("idUsuario")]
        public int IdUsuario { get; set; }

        [Column("turno")]
        public string? Turno { get; set; }
    }
}