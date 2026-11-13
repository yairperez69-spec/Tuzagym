
using System;

namespace Tuzagym.Models
{
    public class ArqueoCajaDto
    {
        // Indica si la consulta fue exitosa
        public bool Exito { get; set; }

        // Identificador de la caja
        public int IdCaja { get; set; }

        // Fecha de la caja
        public DateTime Fecha { get; set; }

        // Abierta o Cerrada
        public string Estado { get; set; } = string.Empty;

        // Dinero ingresado
        public decimal TotalIngresos { get; set; }

        // Dinero gastado
        public decimal TotalEgresos { get; set; }

        // Saldo disponible
        public decimal SaldoActual { get; set; }
    }
}
