using System.ComponentModel.DataAnnotations;

namespace RR_Nueva_Naturaleza.Models
{
    public class Device_Type
    {
        [Key]
        public Guid Id { get; set; }
        [MaxLength(30)]
        public string Device_Type_Name { get; set; } = string.Empty;
    }
}