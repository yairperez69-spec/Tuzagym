
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tuzagym.API.Data
{
    [Table("Socios")]
    public class Socio
    {
        [Key]
        [Column("idUsuario")]
        public int IdUsuario { get; set; }

        [Column("fechaRegistro")]
        public DateTime FechaRegistro { get; set; }

        [Column("telefono")]
        public string? Telefono { get; set; }

        [Column("qrAcceso")]
        public string QrAcceso { get; set; } = string.Empty;

        [Column("saldoSesionesVIP")]
        public int SaldoSesionesVIP { get; set; }
    }
}
