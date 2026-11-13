
using System;

namespace Tuzagym.Models
{
    public class AvanceDiarioDto
    {
        public int IdSesion { get; set; }

        public int IdAsignacion { get; set; }

        public DateTime FechaHora { get; set; }

        public string Estado { get; set; } = "";

        public DateTime? FechaFinalizacion { get; set; }
    }

    public class ResultadoEntrenamientoDto
    {
        public bool Exito { get; set; }

        public string Mensaje { get; set; } = "";

        public int IdSesion { get; set; }
    }
}
