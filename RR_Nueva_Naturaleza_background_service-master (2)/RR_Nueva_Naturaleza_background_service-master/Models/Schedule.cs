using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServicioSensorica.Worker.Models
{
    [Table("Schedules")]
    public class Schedule
    {
        [Key] //Data Annotations
        public Guid Id { get; set; }
        [Required]
        public int Hour { get; set; }        
        [Required]
        public int Minute { get; set; }
        [Required]
        public int DurationSeconds { get; set; }
        // Foreign keys
        [Required]
        public Guid DeviceId { get; set; }
    }
}
