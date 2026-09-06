namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class ScheduleDto
    {
        public Guid Id { get; set; }
        public int Hour { get; set; }
        public int Minute { get; set; }
        public int DurationSeconds { get; set; } 
        public Guid DeviceId { get; set; }
        public string? DeviceName { get; set; }
    }
}
