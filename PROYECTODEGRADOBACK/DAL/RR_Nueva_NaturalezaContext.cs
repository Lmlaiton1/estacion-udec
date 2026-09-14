using Microsoft.EntityFrameworkCore;
using RR_Nueva_Naturaleza.Models;

namespace RR_Nueva_Naturaleza.DAL
{
    public class RR_Nueva_NaturalezaContext : DbContext
    {
        public RR_Nueva_NaturalezaContext(DbContextOptions<RR_Nueva_NaturalezaContext> options) : base(options) { }

        public DbSet<Audit> Audits { get; set; }
        public DbSet<Device> Devices { get; set; }
        public DbSet<Device_Status> Device_States { get; set; }
        public DbSet<Device_Type> Device_Types { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Impact> Impacts { get; set; }
        public DbSet<Measurement> Measurements { get; set; }
        public DbSet<Measurement_Type> Measurement_Types { get; set; }
        public DbSet<Rol> Rols { get; set; }
        public DbSet<System_Type> System_Types { get; set; }
        public DbSet<Unit_Measurement> Unit_Measurements { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<ActuatorMode> ActuatorModes { get; set; }
        public DbSet<ControlRuleActuator> ControlRuleActuators { get; set; }
        public DbSet<ControlRules> ControlRules { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<ChecklistHeader> ChecklistHeaders { get; set; }
        public DbSet<ChecklistDetail> ChecklistDetails { get; set; }
        public DbSet<SerialPortConfig> SerialPortConfigs { get; set; }

        // Dominio Estación Meteorológica
        public DbSet<EstacionMeteo> Estaciones_Meteo { get; set; }
        public DbSet<SensorMeteo> Sensores_Meteo { get; set; }
        public DbSet<LecturaMeteo> Lecturas_Meteo { get; set; }
        public DbSet<LecturaCruda> Lecturas_Crudas { get; set; }
        public DbSet<UmbralMeteo> Umbrales_Meteo { get; set; }
        public DbSet<AlertaMeteo> Alertas_Meteo { get; set; }
        public DbSet<PrediccionMeteo> Predicciones_Meteo { get; set; }
        public DbSet<ImagenMeteo> Imagenes_Meteo { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Índice único en Id_Card
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Id_Card)
                .IsUnique();

            modelBuilder.Entity<Device>()
                .HasIndex(a => a.Device_Name)
                .IsUnique();

            modelBuilder.Entity<Measurement_Type>()
                .HasIndex(b => b.Name)
                .IsUnique();

            modelBuilder.Entity<Unit_Measurement>()
                .HasIndex(c => c.Name)
                .IsUnique();

            modelBuilder.Entity<Impact>()
                .HasIndex(e => e.Impact_Type)
                .IsUnique();

            modelBuilder.Entity<System_Type>()
                .HasIndex(f  => f.System_Type_Name)
                .IsUnique();

            modelBuilder.Entity<Device_Status>()
                .HasIndex(g => g.State_Name)
                .IsUnique();

            modelBuilder.Entity<Device_Type>()
                .HasIndex(i => i.Device_Type_Name)
                .IsUnique();

            // Relaciones ChecklistHeader -> ChecklistDetail
            modelBuilder.Entity<ChecklistHeader>()
                .HasMany(h => h.Detalles)
                .WithOne(d => d.ChecklistHeader)
                .HasForeignKey(d => d.ChecklistHeaderId)
                .OnDelete(DeleteBehavior.Cascade);

            // ChecklistDetail -> Device
            modelBuilder.Entity<ChecklistDetail>()
                .HasOne(d => d.Dispositivo)
                .WithMany()
                .HasForeignKey(d => d.DispositivoId);

            // ChecklistHeader -> User
            modelBuilder.Entity<ChecklistHeader>()
                .HasOne(h => h.Usuario)
                .WithMany()
                .HasForeignKey(h => h.UsuarioId);

            // Dominio Estación Meteorológica
            modelBuilder.Entity<EstacionMeteo>()
                .HasIndex(e => e.Numero)
                .IsUnique();

            // Índice compuesto para acelerar las consultas del dashboard
            modelBuilder.Entity<LecturaMeteo>()
                .HasIndex(l => new { l.EstacionNumero, l.Variable, l.FechaHora });

            // El payload crudo debe ser JSON válido
            modelBuilder.Entity<LecturaCruda>()
                .ToTable(t => t.HasCheckConstraint("CK_Lecturas_Crudas_Payload_ISJSON", "ISJSON(Payload) = 1"));

            // UmbralMeteo -> User (quién lo creó)
            modelBuilder.Entity<UmbralMeteo>()
                .HasOne(u => u.Usuario)
                .WithMany()
                .HasForeignKey(u => u.CreadoPor);

            // AlertaMeteo -> LecturaMeteo
            modelBuilder.Entity<AlertaMeteo>()
                .HasOne(a => a.Lectura)
                .WithMany()
                .HasForeignKey(a => a.LecturaId);

            // AlertaMeteo -> UmbralMeteo
            modelBuilder.Entity<AlertaMeteo>()
                .HasOne(a => a.Umbral)
                .WithMany()
                .HasForeignKey(a => a.UmbralId);

        }
    }
}
