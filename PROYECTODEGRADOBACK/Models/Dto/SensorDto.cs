namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class SensorDto
    {
        public Guid Id { get; set; }
        public string Device_Name { get; set; } = string.Empty;
        public string Mark { get; set; } = string.Empty;
        public string SN { get; set; } = string.Empty;
        public string? Description { get; set; } = string.Empty;
        // foreign keys
        public Guid System_TypeId { get; set; }
        public string Device_StatusName { get; set; } = string.Empty;
        public Guid Device_StatusId { get; set; }
        public Guid unitId { get; set; }
        public decimal? Min { get; set; }
        public decimal? Max { get; set; }
    }
}
