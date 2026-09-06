using System.ComponentModel.DataAnnotations;

namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class DeviceDto
    {
        public Guid Id { get; set; }
        public string Device_Name { get; set; } = string.Empty;
        public string Mark { get; set; } = string.Empty;
        public string SN { get; set; } = string.Empty;
        public string? Description { get; set; } = string.Empty;
        // foreign keys
        public Guid System_TypeId { get; set; }
        public string Device_StatusName { get; set; } = string.Empty;
        public Guid Device_StatusId { get; set; }
        public bool Auto { get; set; } 

    }
}
