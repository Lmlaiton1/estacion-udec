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
    }
}
