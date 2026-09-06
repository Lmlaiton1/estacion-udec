namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class AuditUserPdfRequestDto
    {
        public Guid UserId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string ChartImage { get; set; } // base64 del gráfico
    }
}
