using Microsoft.EntityFrameworkCore;
using ServicioSensorica.Models;

namespace ServicioSensorica.Data
{
    // DbContext propio del control de cultivo, independiente de AppDbContext y WeatherDbContext
    public class CultivoDbContext : DbContext
    {
        public CultivoDbContext(DbContextOptions<CultivoDbContext> options)
            : base(options)
        {
        }

        // Solo lectura: catálogo administrado por el backend
        public DbSet<CultivoW> Cultivos { get; set; }
        public DbSet<EtapaCultivoW> Etapas { get; set; }
        public DbSet<SensorCultivoW> Sensores { get; set; }
        public DbSet<UmbralCultivoW> Umbrales { get; set; }

        // Solo lectura: dispositivos y mediciones de Nueva Naturaleza (acuaponía) vinculados a cultivos
        public DbSet<MedicionNuevaNaturalezaW> MedicionesNuevaNaturaleza { get; set; }
        public DbSet<DispositivoNuevaNaturalezaW> DispositivosNuevaNaturaleza { get; set; }

        // El worker cambia el estado de los actuadores en modo automático
        public DbSet<ActuadorCultivoW> Actuadores { get; set; }

        // Escritas por el worker
        public DbSet<LecturaCultivoW> Lecturas { get; set; }
        public DbSet<AlarmaCultivoW> Alarmas { get; set; }
        public DbSet<BitacoraAlarmaCultivoW> BitacoraAlarmas { get; set; }
        public DbSet<EventoDispositivoCultivoW> Eventos { get; set; }
    }
}
