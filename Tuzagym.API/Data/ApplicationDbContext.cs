
using Microsoft.EntityFrameworkCore;
using Tuzagym.API.Models;

namespace Tuzagym.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // USUARIOS Y PERSONAL
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Socio> Socios { get; set; }
        public DbSet<Entrenador> Entrenadores { get; set; }
        public DbSet<Recepcionista> Recepcionistas { get; set; }

        // RUTINAS Y ENTRENAMIENTOS
        public DbSet<Rutina> Rutinas { get; set; }
        public DbSet<Ejercicio> Ejercicios { get; set; }
        public DbSet<AsignacionRutina> AsignacionesRutinas { get; set; }
        public DbSet<AvanceDiario> AvancesDiarios { get; set; }
        public DbSet<Asistencia> Asistencias { get; set; }
        public DbSet<HistorialSesion> HistorialSesiones { get; set; }

        // MODULO FINANCIERO
        public DbSet<Pago> Pagos { get; set; }
        public DbSet<Membresia> Membresias { get; set; }
        public DbSet<Caja> Cajas { get; set; }
        public DbSet<Comprobante> Comprobantes { get; set; }
        public DbSet<ReporteComisiones> ReportesComisiones { get; set; }
        public DbSet<Gasto> Gastos { get; set; }
    }
}
