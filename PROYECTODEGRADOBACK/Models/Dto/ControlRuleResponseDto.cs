namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class ControlRuleResponseDto
    {
        public Guid Id { get; set; }
        public string? DeviceName { get; set; }
        public decimal? Min { get; set; }
        public decimal? Max { get; set; }
        public Guid DeviceId { get; set; }
        public List<ActuatorDto> Actuators { get; set; } = new();
    }
}
