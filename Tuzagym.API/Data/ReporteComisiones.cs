
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tuzagym.API.Data
{
    [Table("Reporte_Comisiones")]
    public class ReporteComisiones
    {
        [Key]
        [Column("idReporte")]
        public int IdReporte { get; set; }

        [Column("idEntrenador")]
        public int IdEntrenador { get; set; }

        [Column("periodo")]
        public string Periodo { get; set; } = string.Empty;

        [Column("totalSesiones")]
        public int TotalSesiones { get; set; }

        [Column("montoComision", TypeName = "decimal(10,2)")]
        public decimal MontoComision { get; set; }

        [Column("fechaGeneracion")]
        public DateTime FechaGeneracion { get; set; }
    }
}
