using System;
using System.Collections.Generic;
using System.Text;

namespace Tuzagym.Models;

public class Socio
{
    public int SocioId { get; set; }
    public int UsuarioId { get; set; }
    public string CodigoQR { get; set; } = string.Empty;
    public int SesionesVipRestantes { get; set; }
    public DateTime? FechaNacimiento { get; set; }
}