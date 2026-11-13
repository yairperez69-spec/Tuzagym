
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tuzagym.API.Models
{
    [Table("Avance_Diario", Schema = "dbo")]
    public class AvanceDiario
    {
        [Key]
        [Column("idSesion")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdSesion { get; set; }

        [Column("idAsignacion")]
        public int IdAsignacion { get; set; }

        [Column("fechaHora")]
        public DateTime FechaHora { get; set; }

        [Column("estado")]
        public string Estado { get; set; } = "En Proceso";

        [Column("fechaFinalizacion")]
        public DateTime? FechaFinalizacion { get; set; }
    }
}
