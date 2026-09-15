using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServicioSensorica.Models
{
    // Respaldo del JSON tal cual llega por MQTT, antes de parsear
    [Table("Lecturas_Crudas")]
    public class LecturaCruda
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public int EstacionNumero { get; set; }

        [Required]
        [MaxLength(100)]
        public string Topic { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public string Payload { get; set; } = string.Empty;

        [Required]
        public DateTime FechaRecepcion { get; set; }

        [Required]
        public bool Procesado { get; set; }
    }
}
