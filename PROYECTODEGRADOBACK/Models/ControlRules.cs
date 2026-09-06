using System.ComponentModel.DataAnnotations;

namespace RR_Nueva_Naturaleza.Models
{
    public class ControlRules
    {
        [Key] 
        public Guid Id { get; set; }
        public decimal? Max { get; set; }
        public decimal? Min { get; set; }
        public Guid DeviceId { get; set; }
        public virtual Device? Device { get; set; }
        public virtual ICollection<ControlRuleActuator>? ControlRuleActuators { get; set; }
    }
}
