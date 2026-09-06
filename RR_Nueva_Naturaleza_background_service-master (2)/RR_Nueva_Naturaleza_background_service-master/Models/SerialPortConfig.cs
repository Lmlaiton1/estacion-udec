using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServicioSensorica.Worker.Models
{
    [Table("SerialPortConfigs")]
    public class SerialPortConfig
    {
        [Key]
        public Guid Id { get; set; }
        [Required]
        public string PortName { get; set; } = string.Empty;
        [Required]
        public int BaudRate { get; set; }
    }
}
