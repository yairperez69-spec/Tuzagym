
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tuzagym.API.Data
{
    [Table("Caja")]
    public class Caja
    {
        [Key]
        [Column("idCaja")]
        public int IdCaja { get; set; }

        [Column("fecha", TypeName = "date")]
        public DateTime Fecha { get; set; }

        [Column("totalIngresos", TypeName = "decimal(10,2)")]
        public decimal TotalIngresos { get; set; }

        [Column("totalEgresos", TypeName = "decimal(10,2)")]
        public decimal TotalEgresos { get; set; }

        [Column("estado")]
        public string Estado { get; set; } = string.Empty;
    }
}
