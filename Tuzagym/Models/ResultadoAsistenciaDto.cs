
using System;

namespace Tuzagym.Models
{
    public class ResultadoAsistenciaDto
    {
        public bool Exito { get; set; }

        public string Mensaje { get; set; } = string.Empty;

        public string? NombreSocio { get; set; }

        public DateTime? FechaHora { get; set; }

        public int? IdHistorial { get; set; }
    }
}
