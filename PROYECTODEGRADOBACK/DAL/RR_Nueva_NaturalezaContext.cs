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
        public DbSet<AuditoriaMeteo> Auditoria_Meteo { get; set; }

        // Control de umbrales y alarmas de cultivo
        public DbSet<Cultivo> Cultivos { get; set; }
        public DbSet<EtapaCultivo> Etapas_Cultivo { get; set; }
        public DbSet<SensorCultivo> Sensores_Cultivo { get; set; }
        public DbSet<ActuadorCultivo> Actuadores_Cultivo { get; set; }
        public DbSet<UmbralCultivo> Umbrales_Cultivo { get; set; }
        public DbSet<LecturaCultivo> Lecturas_Cultivo { get; set; }
        public DbSet<AlarmaCultivo> Alarmas_Cultivo { get; set; }
        public DbSet<BitacoraAlarmaCultivo> Bitacora_Alarmas_Cultivo { get; set; }
        public DbSet<EventoDispositivoCultivo> Eventos_Dispositivo_Cultivo { get; set; }
        public DbSet<AuditoriaCultivo> Auditoria_Cultivo { get; set; }
        public DbSet<CultivoEstacion> Cultivo_Estaciones { get; set; }

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
            // Numero es clave alterna: permite que LecturaMeteo tenga una FK real contra ella
            modelBuilder.Entity<EstacionMeteo>()
                .HasAlternateKey(e => e.Numero);

            // Índice compuesto para acelerar las consultas del dashboard
            modelBuilder.Entity<LecturaMeteo>()
                .HasIndex(l => new { l.EstacionNumero, l.Variable, l.FechaHora });

            // LecturaMeteo -> EstacionMeteo: solo se guarda si la estación está registrada en el catálogo.
            // Restrict: no se puede borrar una estación mientras tenga lecturas asociadas.
            modelBuilder.Entity<LecturaMeteo>()
                .HasOne<EstacionMeteo>()
                .WithMany()
                .HasForeignKey(l => l.EstacionNumero)
                .HasPrincipalKey(e => e.Numero)
                .OnDelete(DeleteBehavior.Restrict);

            // LecturaMeteo -> SensorMeteo: solo se guarda si el sensor está registrado en el catálogo.
            modelBuilder.Entity<LecturaMeteo>()
                .HasOne<SensorMeteo>()
                .WithMany()
                .HasForeignKey(l => l.SensorId)
                .OnDelete(DeleteBehavior.Restrict);

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

            // AlertaMeteo -> User (quién la marcó como vista). Restrict: conserva el rastro.
            modelBuilder.Entity<AlertaMeteo>()
                .HasOne(a => a.VistoPor)
                .WithMany()
                .HasForeignKey(a => a.VistoPorId)
                .OnDelete(DeleteBehavior.Restrict);

            // AuditoriaMeteo -> User (quién activó/desactivó). Restrict: conserva el rastro aunque el usuario se borre.
            modelBuilder.Entity<AuditoriaMeteo>()
                .HasOne(a => a.Usuario)
                .WithMany()
                .HasForeignKey(a => a.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            // Sensores y actuadores de cultivo vinculados a dispositivos de Nueva Naturaleza (acuaponía)
            modelBuilder.Entity<SensorCultivo>()
                .HasOne(s => s.Device)
                .WithMany()
                .HasForeignKey(s => s.DeviceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ActuadorCultivo>()
                .HasOne(a => a.Device)
                .WithMany()
                .HasForeignKey(a => a.DeviceId)
                .OnDelete(DeleteBehavior.Restrict);

            // Control de cultivo
            // Todo en Restrict (salvo etapas y bitácora) para conservar la auditoría y evitar
            // múltiples rutas de borrado en cascada en SQL Server.
            modelBuilder.Entity<Cultivo>()
                .HasIndex(c => c.Numero)
                .IsUnique()
                .HasFilter("[Eliminado] = 0");

            modelBuilder.Entity<Cultivo>()
                .HasOne(c => c.Usuario)
                .WithMany()
                .HasForeignKey(c => c.CreadoPor)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EtapaCultivo>()
                .HasOne(e => e.Cultivo)
                .WithMany(c => c.Etapas)
                .HasForeignKey(e => e.CultivoId)
                .OnDelete(DeleteBehavior.Cascade);

            // La clave es única dentro de cada cultivo: es el nombre del campo en el JSON del ESP
            modelBuilder.Entity<SensorCultivo>()
                .HasIndex(s => new { s.CultivoId, s.Clave })
                .IsUnique()
                .HasFilter("[Eliminado] = 0");

            modelBuilder.Entity<SensorCultivo>()
                .HasOne(s => s.Cultivo)
                .WithMany()
                .HasForeignKey(s => s.CultivoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ActuadorCultivo>()
                .HasIndex(a => new { a.CultivoId, a.Clave })
                .IsUnique()
                .HasFilter("[Eliminado] = 0");

            modelBuilder.Entity<ActuadorCultivo>()
                .HasOne(a => a.Cultivo)
                .WithMany()
                .HasForeignKey(a => a.CultivoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UmbralCultivo>()
                .HasOne(u => u.Cultivo)
                .WithMany()
                .HasForeignKey(u => u.CultivoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UmbralCultivo>()
                .HasOne(u => u.Sensor)
                .WithMany()
                .HasForeignKey(u => u.SensorCultivoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UmbralCultivo>()
                .HasOne(u => u.Etapa)
                .WithMany()
                .HasForeignKey(u => u.EtapaCultivoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UmbralCultivo>()
                .HasOne(u => u.Actuador)
                .WithMany()
                .HasForeignKey(u => u.ActuadorCultivoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UmbralCultivo>()
                .HasOne(u => u.Usuario)
                .WithMany()
                .HasForeignKey(u => u.CreadoPor)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LecturaCultivo>()
                .HasIndex(l => new { l.SensorCultivoId, l.FechaHora });

            modelBuilder.Entity<LecturaCultivo>()
                .HasOne<SensorCultivo>()
                .WithMany()
                .HasForeignKey(l => l.SensorCultivoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AlarmaCultivo>()
                .HasIndex(a => new { a.CultivoId, a.Estado });

            modelBuilder.Entity<AlarmaCultivo>()
                .HasOne(a => a.Cultivo)
                .WithMany()
                .HasForeignKey(a => a.CultivoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AlarmaCultivo>()
                .HasOne(a => a.Umbral)
                .WithMany()
                .HasForeignKey(a => a.UmbralCultivoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AlarmaCultivo>()
                .HasOne(a => a.VistaPor)
                .WithMany()
                .HasForeignKey(a => a.VistaPorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AlarmaCultivo>()
                .HasOne(a => a.AtendidaPor)
                .WithMany()
                .HasForeignKey(a => a.AtendidaPorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AlarmaCultivo>()
                .HasOne(a => a.ResueltaPor)
                .WithMany()
                .HasForeignKey(a => a.ResueltaPorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BitacoraAlarmaCultivo>()
                .HasOne(b => b.Alarma)
                .WithMany(a => a.Bitacora)
                .HasForeignKey(b => b.AlarmaCultivoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BitacoraAlarmaCultivo>()
                .HasOne(b => b.Usuario)
                .WithMany()
                .HasForeignKey(b => b.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EventoDispositivoCultivo>()
                .HasIndex(e => new { e.CultivoId, e.Fecha });

            modelBuilder.Entity<EventoDispositivoCultivo>()
                .HasOne(e => e.Usuario)
                .WithMany()
                .HasForeignKey(e => e.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            // Cultivo <-> Estaciones (muchos a muchos)
            modelBuilder.Entity<CultivoEstacion>()
                .HasKey(ce => new { ce.CultivoId, ce.EstacionNumero });

            modelBuilder.Entity<CultivoEstacion>()
                .HasOne(ce => ce.Cultivo)
                .WithMany(c => c.Estaciones)
                .HasForeignKey(ce => ce.CultivoId)
                .OnDelete(DeleteBehavior.Cascade);

            // Restrict: no se puede borrar una estación mientras un cultivo la use
            modelBuilder.Entity<CultivoEstacion>()
                .HasOne<EstacionMeteo>()
                .WithMany()
                .HasForeignKey(ce => ce.EstacionNumero)
                .HasPrincipalKey(e => e.Numero)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AuditoriaCultivo>()
                .HasIndex(a => new { a.CultivoId, a.Fecha });

            modelBuilder.Entity<AuditoriaCultivo>()
                .HasOne(a => a.Usuario)
                .WithMany()
                .HasForeignKey(a => a.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

        }
    }
}
