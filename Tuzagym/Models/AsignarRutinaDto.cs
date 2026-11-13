
namespace Tuzagym.Models
{
    public class AsignarRutinaDto
    {
        public int IdUsuario { get; set; }
        public int IdEntrenador { get; set; }
        public string NombreRutina { get; set; } = string.Empty;
        public int Series { get; set; }
        public string Repeticiones { get; set; } = string.Empty;
        public string? ComentarioEntrenador { get; set; }
    }
}
