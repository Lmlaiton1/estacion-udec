using System.ComponentModel.DataAnnotations;

namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class AuditDto
    {
        public Guid Id { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Date { get; set; }
        public string? Observation { get; set; } = string.Empty;
        // foreign keys
        public Guid UserId { get; set; }
        public Guid DeviceId { get; set; }
    }
}
