namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class ChecklistCsvRequestDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public Guid? UserId { get; set; }
    }
}
