namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class SensorPostDto
    {
        public string Device_Name { get; set; } = string.Empty;
        public string Mark { get; set; } = string.Empty;
        public string SN { get; set; } = string.Empty;
        public string? Description { get; set; } = string.Empty;
        // foreign keys
        public Guid System_TypeId { get; set; }
        public Guid UnitId { get; set; }
        public decimal Min { get; set; }
        public decimal Max { get; set; }
        public Guid Device_StatusId { get; set; }
    }
}
