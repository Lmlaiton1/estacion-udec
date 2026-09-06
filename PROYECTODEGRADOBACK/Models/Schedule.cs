using System.ComponentModel.DataAnnotations;

namespace RR_Nueva_Naturaleza.Models
{
    public class Schedule
    {
        [Key]
        public Guid Id { get; set; }
        public int Hour { get; set; }
        public int Minute { get; set; }
        public int DurationSeconds { get; set; } 
        public Guid DeviceId { get; set; }
        public virtual Device? Device { get; set; }

    }
}
