
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tuzagym.API.Data
{
    [Table("Gastos")]
    public class Gasto
    {
        // IDENTIFICADOR DEL GASTO
        [Key]
        [Column("idGasto")]
        public int IdGasto { get; set; }

        // CAJA A LA QUE PERTENECE
        [Required]
        [Column("idCaja")]
        public int IdCaja { get; set; }

        // DESCRIPCION DEL GASTO
        [Required]
        [MaxLength(200)]
        [Column("concepto")]
        public string Concepto { get; set; } = string.Empty;

        // CANTIDAD GASTADA
        [Column("monto", TypeName = "decimal(10,2)")]
        public decimal Monto { get; set; }

        // FECHA DEL GASTO
        [Column("fecha")]
        public DateTime Fecha { get; set; }

        // USUARIO QUE REGISTRO EL GASTO
        [Required]
        [MaxLength(150)]
        [Column("responsable")]
        public string Responsable { get; set; } = string.Empty;

        // RELACION CON CAJA
        [ForeignKey(nameof(IdCaja))]
        public Caja? Caja { get; set; }
    }
}
