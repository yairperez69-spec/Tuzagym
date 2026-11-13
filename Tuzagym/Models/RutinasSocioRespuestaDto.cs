
using System;
using System.Collections.Generic;

namespace Tuzagym.Models
{
    public class RutinasSocioRespuestaDto
    {
        public bool TieneRutinas { get; set; }

        public List<RutinaAsignadaSocioDto> Rutinas { get; set; }
            = new List<RutinaAsignadaSocioDto>();
    }

    public class RutinaAsignadaSocioDto
    {
        public int IdAsignacion { get; set; }

        public int IdRutina { get; set; }

        public string NombreRutina { get; set; } = "";

        public string Objetivo { get; set; } = "";

        public string Nivel { get; set; } = "";

        public DateTime? FechaAsignacion { get; set; }

        public string? ComentarioEntrenador { get; set; }
        public List<EjercicioSocioDto> Ejercicios { get; set; }
            = new List<EjercicioSocioDto>();
    }
}
