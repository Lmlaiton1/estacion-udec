namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class EventCreateDto
    {
        public string State { get; set; } = string.Empty;
        public string? DeviceName { get; set; } = string.Empty;
        public Guid DeviceId { get; set; }
    }
}
