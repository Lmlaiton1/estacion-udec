using Microsoft.EntityFrameworkCore;
using ServicioSensorica.Models;

namespace ServicioSensorica.Data
{
    // DbContext propio del dominio Estación Meteorológica, independiente de AppDbContext
    public class WeatherDbContext : DbContext
    {
        public WeatherDbContext(DbContextOptions<WeatherDbContext> options)
            : base(options)
        {
        }

        public DbSet<LecturaMeteo> LecturasMeteo { get; set; }
        public DbSet<LecturaCruda> LecturasCrudas { get; set; }

        // Solo lectura: el catálogo lo administra el backend, el worker únicamente valida existencia.
        public DbSet<EstacionMeteo> EstacionesMeteo { get; set; }
        public DbSet<SensorMeteo> SensoresMeteo { get; set; }
        public DbSet<SensorMeteoInactivo> SensoresMeteoInactivos { get; set; }

        // Solo lectura: los umbrales los administra el backend. AlertasMeteo sí la escribe el worker.
        public DbSet<UmbralMeteo> UmbralesMeteo { get; set; }
        public DbSet<AlertaMeteo> AlertasMeteo { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SensorMeteoInactivo>()
                .HasKey(i => new { i.SensorId, i.EstacionNumero });
        }

        // Sensores que cuentan para la estación: activos en general y no desactivados puntualmente en ella
        public async Task<HashSet<Guid>> SensoresActivosEnEstacionAsync(int estacionNumero, CancellationToken ct)
        {
            var ids = await SensoresMeteo
                .Where(s => s.Activo && !SensoresMeteoInactivos.Any(i => i.SensorId == s.Id && i.EstacionNumero == estacionNumero))
                .Select(s => s.Id)
                .ToListAsync(ct);
            return ids.ToHashSet();
        }
    }
}
