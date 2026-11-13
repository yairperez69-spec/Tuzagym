
using System;
using System.ComponentModel.DataAnnotations;

namespace Tuzagym.API.Models
{
    public class AsignarRutinaDto
    {
        [Range(1, int.MaxValue)]
        public int IdUsuario { get; set; }

        [Range(1, int.MaxValue)]
        public int IdEntrenador { get; set; }

        [Required]
        [StringLength(100)]
        public string NombreRutina { get; set; } = string.Empty;

        [Range(1, 20)]
        public int Series { get; set; }

        [Required]
        public string Repeticiones { get; set; } = string.Empty;
    }
}
