
namespace Tuzagym.Models
{
    public class SocioEntrenador
    {
        public int IdUsuario { get; set; }

        public string Nombre { get; set; } = "";

        public string Correo { get; set; } = "";

        public string Estado { get; set; } = "";

        public string? TipoMembresia { get; set; }

        public override string ToString()
        {
            return Nombre;
        }
    }
}
