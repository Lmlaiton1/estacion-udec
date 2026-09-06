using System.ComponentModel.DataAnnotations;

namespace RR_Nueva_Naturaleza.Models
{
    public class ActuatorMode
    {

        [Key] //Data Annotations
        public Guid Id { get; set; }
        public bool SupportsAuto { get; set; }
        public bool IsAutoMode { get; set; }
        public Guid DeviceId { get; set; }
        public virtual Device? Device { get; set; }

    }
}
