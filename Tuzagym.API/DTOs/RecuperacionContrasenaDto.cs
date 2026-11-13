
using System.ComponentModel.DataAnnotations;

namespace Tuzagym.API.DTOs
{
    public class SolicitarRecuperacionDto
    {
        [Required]
        [EmailAddress]
        public string Correo { get; set; } = "";
    }

    public class RestablecerContrasenaDto
    {
        [Required]
        [EmailAddress]
        public string Correo { get; set; } = "";

        [Required]
        [RegularExpression(@"^\d{6}$")]
        public string Codigo { get; set; } = "";

        [Required]
        [MinLength(8)]
        public string NuevaContrasena { get; set; } = "";
    }
}
