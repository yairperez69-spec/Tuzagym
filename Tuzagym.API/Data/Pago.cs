
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tuzagym.API.Data
{
    [Table("Pago")]
    public class Pago
    {
        [Key]
        [Column("idPago")]
        public int IdPago { get; set; }

        [Column("idMembresia")]
        public int IdMembresia { get; set; }

        [Column("idCaja")]
        public int IdCaja { get; set; }

        [Column("fecha")]
        public DateTime Fecha { get; set; }

        [Column("monto", TypeName = "decimal(10,2)")]
        public decimal Monto { get; set; }

        [Column("metodo")]
        public string Metodo { get; set; } = string.Empty;

        [Column("estado")]
        public string Estado { get; set; } = string.Empty;
    }
}
