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

        // Solo lectura: los umbrales los administra el backend. AlertasMeteo sí la escribe el worker.
        public DbSet<UmbralMeteo> UmbralesMeteo { get; set; }
        public DbSet<AlertaMeteo> AlertasMeteo { get; set; }
    }
}
