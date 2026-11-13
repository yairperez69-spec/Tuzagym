
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tuzagym.API.Models;

[Table("Asignacion_Rutina")]
public class AsignacionRutina
{
    [Key]
    [Column("idAsignacion")]
    public int IdAsignacion { get; set; }

    [Column("idSocio")]
    public int IdSocio { get; set; }

    [Column("idRutina")]
    public int IdRutina { get; set; }

    [Column("fechaAsignacion")]
    public DateTime FechaAsignacion { get; set; } = DateTime.Now;

    [Column("estado")]
    public string Estado { get; set; } = "Activa";

    // Comentario personalizado del entrenador para este socio
    [Column("comentarioEntrenador")]
    [MaxLength(500)]
    public string? ComentarioEntrenador { get; set; }
}
