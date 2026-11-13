
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tuzagym.API.Data
{
    [Table("Membresia")]
    public class Membresia
    {
        [Key]
        [Column("idMembresia")]
        public int IdMembresia { get; set; }

        [Column("idSocio")]
        public int IdSocio { get; set; }

        [Column("tipo")]
        public string Tipo { get; set; } = string.Empty;

        [Column("fechaInicio", TypeName = "date")]
        public DateTime FechaInicio { get; set; }

        [Column("fechaFin", TypeName = "date")]
        public DateTime FechaFin { get; set; }

        [Column("precio", TypeName = "decimal(10,2)")]
        public decimal Precio { get; set; }

        [Column("estado")]
        public string Estado { get; set; } = string.Empty;
    }
}
