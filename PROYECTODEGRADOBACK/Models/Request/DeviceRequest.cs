namespace RR_Nueva_Naturaleza.Models.Request
{
    public class DeviceRequest
    {
        public Guid Id { get; set; }
        public string Device_Name { get; set; } = string.Empty;
        // foreign keys
        public Guid SystemId { get; set; }
        public string? SystemName { get; set; }
        public Guid Device_StatusId { get; set; }
        public string? Device_StatusName { get; set; }
    }
}
