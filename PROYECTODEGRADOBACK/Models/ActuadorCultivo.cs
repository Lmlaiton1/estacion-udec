using System.ComponentModel.DataAnnotations;

namespace RR_Nueva_Naturaleza.Models
{
    // Ventilador, riego, nebulizador... Se controla por MQTT en cultivo/{Numero}/actuadores/{Clave}/cmd
    public class ActuadorCultivo
    {
        [Key]
        public Guid Id { get; set; }

        // foreign keys
        public Guid CultivoId { get; set; }
        public virtual Cultivo? Cultivo { get; set; }

        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Clave { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Tipo { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Marca { get; set; }

        [MaxLength(50)]
        public string? Potencia { get; set; }

        [MaxLength(500)]
        public string? Especificaciones { get; set; }

        // "Automatico": lo manejan los umbrales | "Manual": solo lo maneja un usuario
        [MaxLength(20)]
        public string Modo { get; set; } = ModosActuador.Automatico;

        public bool Encendido { get; set; } = false;

        public bool Activo { get; set; } = true;

        // Eliminación lógica: se oculta de las listas pero se conserva para la auditoría
        public bool Eliminado { get; set; } = false;

        public DateTime? FechaUltimoCambio { get; set; }

        // Actuador de Nueva Naturaleza (acuaponía) vinculado. Si tiene valor, se acciona por el puerto
        // serial del worker de acuaponía (ReceiveCommand) en vez del MQTT del ESP.
        public Guid? DeviceId { get; set; }
        public virtual Device? Device { get; set; }
    }

    public static class ModosActuador
    {
        public const string Automatico = "Automatico";
        public const string Manual = "Manual";
    }
}
