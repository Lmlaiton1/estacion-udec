using System.ComponentModel.DataAnnotations;

namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class EventDto
    {
        public Guid Id { get; set; }
        public string Date { get; set; }
        public string Notification { get; set; } = string.Empty;
        public bool Visto { get; set; } = false;
        // foreign keys
        public Guid DeviceId { get; set; }
        public Guid ImpactId { get; set; }
        public Guid SystemId { get; set; }
    }
}
