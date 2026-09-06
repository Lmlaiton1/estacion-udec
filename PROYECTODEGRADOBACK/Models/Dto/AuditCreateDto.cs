namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class AuditCreateDto
    {
        public string Action { get; set; } = string.Empty;
        public string? Observation { get; set; } = string.Empty;
        public Guid DeviceId { get; set; }

    }
}
