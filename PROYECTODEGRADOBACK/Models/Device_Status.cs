using System.ComponentModel.DataAnnotations;

namespace RR_Nueva_Naturaleza.Models
{
    public class Device_Status
    {
        [Key]
        public Guid Id { get; set; }
        [MaxLength(30)]
        public string State_Name { get; set; } = string.Empty;
    }
}
