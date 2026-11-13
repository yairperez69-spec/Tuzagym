
using System;

namespace Tuzagym.Models
{
    public class ResumenFinancieroDto
    {
        public decimal VentasMes { get; set; }
        public decimal GastosMes { get; set; }
        public decimal GananciasMes { get; set; }
        public decimal SaldoCaja { get; set; }

        public string EstadoCaja { get; set; }
            = "Sin registros";

        public DateTime FechaConsulta { get; set; }

        public bool HayDatosCaja { get; set; }
    }
}
