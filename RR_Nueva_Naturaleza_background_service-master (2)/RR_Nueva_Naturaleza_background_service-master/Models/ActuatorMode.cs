using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServicioSensorica.Models
{
    [Table("ActuatorModes")]
    public class ActuatorMode
    {

        [Key] //Data Annotations
        public Guid Id { get; set; }
        [Required]
        public bool SupportsAuto { get; set; }
        [Required]
        public bool IsAutoMode { get; set; }

        // Foreign keys
        [Required]
        public Guid DeviceId { get; set; }

    }
}