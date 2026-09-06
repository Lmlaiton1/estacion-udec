namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class ChecklistPdfRequestDto
    {
        public Guid SensorId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string ChartImage { get; set; }
    }
}
