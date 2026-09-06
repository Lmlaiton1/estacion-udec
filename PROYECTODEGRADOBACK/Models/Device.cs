using System.ComponentModel.DataAnnotations;

namespace RR_Nueva_Naturaleza.Models
{
    public class Device
    {
        [Key]
        public Guid Id { get; set; }
        [MaxLength(50)]
        public string Device_Name { get; set; } = string.Empty;
        [MaxLength(30)]
        public string Mark { get; set; } = string.Empty;
        [MaxLength(30)]
        public string SN { get; set; } = string.Empty;
        public string? Description { get; set; } = string.Empty;
        // foreign keys
        public Guid System_TypeId { get; set; }
        public virtual System_Type? System_Type { get; set; }
        public Guid Device_TypeId { get; set; }
        public virtual Device_Type? Device_Type { get; set; }
        public Guid Device_StatusId { get; set; }
        public virtual Device_Status? Device_Status { get; set; }
    }
}
