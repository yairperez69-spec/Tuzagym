
using System.ComponentModel.DataAnnotations;

namespace Tuzagym.API.DTOs
{
    public class RegistrarGastoDto
    {
        [Required]
        [StringLength(200, MinimumLength = 3)]
        public string Concepto { get; set; } = string.Empty;

        [Range(
            typeof(decimal),
            "0.01",
            "99999999.99",
            ErrorMessage = "El monto debe ser mayor que cero.")]
        public decimal Monto { get; set; }
    }
}
