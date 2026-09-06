namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class ControlRuleCreateDto
    {
        public decimal Min { get; set; }
        public decimal Max { get; set; }
        public List<ActuatorDto> Actuators { get; set; } = new();
    }
}
