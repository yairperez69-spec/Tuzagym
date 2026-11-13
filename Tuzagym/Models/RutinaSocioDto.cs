
using System;
using System.Collections.Generic;

namespace Tuzagym.Models
{
    public class RutinaSocioDto
    {
        public bool TieneRutina { get; set; }

        public int IdAsignacion { get; set; }

        public int IdRutina { get; set; }

        public string NombreRutina { get; set; } = "";

        public string? Descripcion { get; set; }

        public string? Objetivo { get; set; }

        public string? Mensaje { get; set; }

        public DateTime? FechaAsignacion { get; set; }

        public List<EjercicioSocioDto> Ejercicios { get; set; } = new();
    }

    public class EjercicioSocioDto
    {
        public int IdEjercicio { get; set; }

        public string Nombre { get; set; } = "";

        public int Series { get; set; }

        public int Repeticiones { get; set; }

        public int DescansoSeg { get; set; }

        public string? ComentarioEntrenador { get; set; }
    }
}
