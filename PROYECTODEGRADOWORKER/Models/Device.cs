using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServicioSensorica.Models
{
    [Table("Devices")] // <- nombre de la tabla en SQL Server. asegura que mapeamos a la tabla que ya existe en tu BD.
    public class Device
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string Device_Name { get; set; } = string.Empty;

        [Required]
        public string Mark { get; set; } = string.Empty;

        [Required]
        public string SN { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        // Foreign keys
        [Required]
        public Guid System_TypeId { get; set; }

        [Required]
        public Guid Device_TypeId { get; set; }

        [Required]
        public Guid Device_StatusId { get; set; }
    }
}
