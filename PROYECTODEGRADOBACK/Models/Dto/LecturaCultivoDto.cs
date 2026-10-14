namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class LecturaCultivoDto
    {
        public Guid Id { get; set; }
        public Guid CultivoId { get; set; }
        public Guid SensorCultivoId { get; set; }
        public string Variable { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public string Unidad { get; set; } = string.Empty;
        public DateTime FechaHora { get; set; }
    }
}
