namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class LecturaMeteoDto
    {
        public Guid Id { get; set; }
        public int EstacionNumero { get; set; }
        public Guid SensorId { get; set; }
        public string Variable { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public string? ValorCardinal { get; set; }
        public string Unidad { get; set; } = string.Empty;
        public DateTime FechaHora { get; set; }
        public DateTime FechaRecepcion { get; set; }
    }
}
