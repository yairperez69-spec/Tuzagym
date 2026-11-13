
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tuzagym.API.Data
{
    [Table("Comprobante")]
    public class Comprobante
    {
        [Key]
        [Column("idComprobante")]
        public int IdComprobante { get; set; }

        [Column("idPago")]
        public int? IdPago { get; set; }

        [Column("idSocio")]
        public int IdSocio { get; set; }

        [Column("folio")]
        public string Folio { get; set; } = string.Empty;

        [Column("pesoUtilizado", TypeName = "decimal(5,2)")]
        public decimal? PesoUtilizado { get; set; }

        [Column("totalDecimal", TypeName = "decimal(10,2)")]
        public decimal TotalDecimal { get; set; }

        [Column("fechaEmision")]
        public DateTime FechaEmision { get; set; }
    }
}
