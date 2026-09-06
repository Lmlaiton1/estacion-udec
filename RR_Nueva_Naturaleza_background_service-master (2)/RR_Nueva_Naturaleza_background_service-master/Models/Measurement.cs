using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServicioSensorica.Models
{
    [Table("Measurements")] // <- nombre de la tabla en SQL Server. asegura que mapeamos a la tabla que ya existe en tu BD.
    public class Measurement
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public decimal Value { get; set; }

        [Required]
        public string Date { get; set; } = string.Empty;

        // Foreign keys
        [Required]
        public Guid DeviceId { get; set; }

        [Required]
        public Guid Measurement_TypeId { get; set; }
    }
}
