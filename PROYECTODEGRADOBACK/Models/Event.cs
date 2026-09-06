using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RR_Nueva_Naturaleza.Models
{
    public class Event
    {
        [Key]
        public Guid Id { get; set; }
        public string Date { get; set; }
        public string Notification { get; set; } = string.Empty;
        public bool Visto { get; set; } = false;
        // foreign keys
        public Guid DeviceId { get; set; }
        public virtual Device? Device { get; set; }
        public Guid ImpactId { get; set; }
        public virtual Impact? Impact { get; set; }
        public Guid System_TypeId { get; set; }
        public virtual System_Type? System_Type { get; set; }
    }
}
