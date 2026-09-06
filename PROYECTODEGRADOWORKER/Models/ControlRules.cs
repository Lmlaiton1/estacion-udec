using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServicioSensorica.Worker.Models
{
    [Table("ControlRules")]
    public class ControlRules
    {

        [Key] //Data Annotations
        public Guid Id { get; set; }
        [Required]
        public decimal? Max { get; set; }
        [Required]
        public decimal? Min { get; set; }
        // Foreign keys
        [Required]
        public Guid DeviceId { get; set; }

    }
}
