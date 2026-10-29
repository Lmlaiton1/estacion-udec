using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RR_Nueva_Naturaleza.Models
{
    // Un registro por variable medida. Un DHT22 se registra como dos sensores: temperatura y humedad.
    public class SensorCultivo
    {
        [Key]
        public Guid Id { get; set; }

        // foreign keys
        public Guid CultivoId { get; set; }
        public virtual Cultivo? Cultivo { get; set; }

        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        // Llave del campo en el JSON que publica el ESP en cultivo/{Numero}/datos, ej. "temperatura"
        [MaxLength(50)]
        public string Clave { get; set; } = string.Empty;

        // Modelo o tipo de sensor, ej. "DHT22"
        [MaxLength(50)]
        public string Tipo { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Variable { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Unidad { get; set; } = string.Empty;

        // Especificaciones técnicas: rango de medida y precisión según la hoja de datos
        [Column(TypeName = "decimal(10,4)")]
        public decimal? RangoMin { get; set; }

        [Column(TypeName = "decimal(10,4)")]
        public decimal? RangoMax { get; set; }

        [Column(TypeName = "decimal(10,4)")]
        public decimal? Precision { get; set; }

        [MaxLength(50)]
        public string? Marca { get; set; }

        [MaxLength(500)]
        public string? Especificaciones { get; set; }

        public bool Activo { get; set; } = true;

        // Eliminación lógica: se oculta de las listas pero se conserva para la auditoría
        public bool Eliminado { get; set; } = false;

        public DateTime FechaInstalacion { get; set; }

        // Sensor de Nueva Naturaleza (acuaponía) vinculado. Si tiene valor, las lecturas se toman de
        // Measurements de ese dispositivo (puerto serial) en vez del MQTT del ESP.
        public Guid? DeviceId { get; set; }
        public virtual Device? Device { get; set; }
    }
}
