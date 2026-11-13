using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tuzagym.API.Data
{
    [Table("Entrenadores")]
    public class Entrenador
    {
        [Key]
        [Column("idUsuario")]
        public int IdUsuario { get; set; }

        [Column("especialidad")]
        public string? Especialidad { get; set; }

        [Column("limiteClientes")]
        public int LimiteClientes { get; set; }
    }
}