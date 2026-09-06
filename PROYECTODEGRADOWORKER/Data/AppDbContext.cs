using Microsoft.EntityFrameworkCore;
using ServicioSensorica.Models;
using ServicioSensorica.Worker.Models;

namespace ServicioSensorica.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Measurement> Measurements { get; set; }
        public DbSet<Device> Devices { get; set; }
        public DbSet<Unit_Measurement> Unit_Measurements { get; set; }
        public DbSet<System_Type> System_Types { get; set; }
        public DbSet<Impact> Impacts { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Device_State> Device_States { get; set; }
        public DbSet<Measurement_Type> Measurement_Types { get; set; }
        public DbSet<ActuatorMode> ActuatorModes { get; set; }
        public DbSet<ControlRules> ControlRules { get; set; }
        public DbSet<ControlRuleActuator> ControlRuleActuators { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<SerialPortConfig> SerialPortConfigs { get; set; }

    }
}
