namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class SensorMeasurementDto
    {
        public Guid DeviceId { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public string? Mark { get; set; }
        public string? SN { get; set; }
        public List<LastMeasurementDto> Measurements { get; set; } = new();
    }
}
