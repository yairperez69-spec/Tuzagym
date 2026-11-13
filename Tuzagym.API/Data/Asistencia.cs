
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tuzagym.API.Data
{
    [Table("Asistencias")]
    public class Asistencia
    {
        [Key]
        [Column("idAsistencia")]
        public int IdAsistencia { get; set; }

        [Required]
        [Column("idUsuario")]
        public int IdUsuario { get; set; }

        [Column("fechaHoraEntrada")]
        public DateTime FechaHoraEntrada { get; set; }
    }
}
