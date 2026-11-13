
using System;

namespace Tuzagym.Models
{
    public class RutinaAsignadaDto
    {
        public string? ComentarioEntrenador { get; set; }

        public int IdAsignacion { get; set; }
        public int IdRutina { get; set; }
        public int IdSocio { get; set; }

        public string Alumno { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string Objetivo { get; set; } = "";
        public string Nivel { get; set; } = "";

        public int Series { get; set; }
        public int Repeticiones { get; set; }
        public int DescansoSeg { get; set; }

        public DateTime Fecha { get; set; }
        public string Estado { get; set; } = "";

        public string Resumen =>
            $"{Series} series × {Repeticiones} repeticiones • {Estado}";
    }
}
