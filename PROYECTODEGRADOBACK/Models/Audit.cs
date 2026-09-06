using System.ComponentModel.DataAnnotations;

namespace RR_Nueva_Naturaleza.Models
{
    public class Audit
    {
        [Key] //Data Annotations
        public Guid Id { get; set; }
        public string Action { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string? Observation { get; set; } = string.Empty;
        // foreign keys
        public Guid UserId { get; set; }
        public virtual User? User { get; set; }
        public Guid DeviceId { get; set; }
        public virtual Device? Device { get; set; }
    }
}
