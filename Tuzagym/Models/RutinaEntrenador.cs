
using System;

namespace Tuzagym.Models
{
    public class RutinaEntrenador
    {
        public string Alumno { get; set; } = string.Empty;

        public string Nombre { get; set; } = string.Empty;

        public int Series { get; set; }

        public string Repeticiones { get; set; } = string.Empty;

        public DateTime Fecha { get; set; }

        public string Resumen =>
            $"{Series} series × {Repeticiones} repeticiones";
    }
}
