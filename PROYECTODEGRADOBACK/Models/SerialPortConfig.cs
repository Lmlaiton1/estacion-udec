using System.ComponentModel.DataAnnotations;

namespace RR_Nueva_Naturaleza.Models
{
    public class SerialPortConfig
    {
        [Key]
        public Guid Id { get; set; }
        public string PortName { get; set; } = string.Empty;
        public int BaudRate { get; set; } 
    }
}
