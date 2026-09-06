using System.ComponentModel.DataAnnotations;

namespace RR_Nueva_Naturaleza.Models
{
    public class ControlRuleActuator
    {
        [Key]
        public Guid Id { get; set; }
        public Guid ControlRuleId { get; set; }
        public ControlRules? ControlRule { get; set; }
        public Guid DeviceId { get; set; }
        public Device? Device { get; set; }
        public string? TriggerType { get; set; }
    }
}
