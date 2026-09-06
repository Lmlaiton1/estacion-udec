using ServicioSensorica.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServicioSensorica.Worker.Models
{
    [Table("ControlRuleActuators")]
    public class ControlRuleActuator
    {
        [Key]
        public Guid Id { get; set; }
        [Required]
        public Guid ControlRuleId { get; set; }
        [Required]
        public Guid DeviceId { get; set; }
        [Required]
        public string? TriggerType { get; set; }
    }
}
