namespace Tuzagym.Models
{
    public class RegistroSocioDto
    {
        public string NombreCompleto { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string TipoMembresia { get; set; } = "Normal";
        public int? IdEntrenadorPersonal { get; set; }
        public string MetodoPago { get; set; } = "Efectivo";
        public decimal MontoPagado { get; set; }
    }

    public class EntrenadorDto
    {
        public int Id { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
    }

    public class RegistroSocioResult
    { 
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public string FolioQr { get; set; } = string.Empty;
    }

    // Agregamos el DTO que solicita ApiService.cs
    public class ConfirmarCodigoDto
    {
        public string Correo { get; set; } = string.Empty;
        public string CodigoVerificacion { get; set; } = string.Empty;
    }
}