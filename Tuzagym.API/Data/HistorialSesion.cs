
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tuzagym.API.Data
{
    [Table("Historial_Sesiones")]
    public class HistorialSesion
    {
        [Key]
        [Column("idHistorial")]
        public int IdHistorial { get; set; }

        [Required]
        [Column("idSocio")]
        public int IdSocio { get; set; }

        [Column("idEntrenador")]
        public int? IdEntrenador { get; set; }

        [Required]
        [Column("fechaConsulta")]
        public DateTime FechaConsulta { get; set; }

        [Required]
        [Column("tipoEntrada")]
        [MaxLength(50)]
        public string TipoEntrada { get; set; } = string.Empty;
    }
}
