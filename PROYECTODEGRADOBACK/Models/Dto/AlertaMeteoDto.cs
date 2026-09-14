namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class AlertaMeteoDto
    {
        public Guid Id { get; set; }
        public Guid LecturaId { get; set; }
        public Guid UmbralId { get; set; }
        public int EstacionNumero { get; set; }
        public string Variable { get; set; } = string.Empty;
        public decimal ValorRegistrado { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public string NivelGravedad { get; set; } = string.Empty;
        public DateTime FechaHora { get; set; }
        public bool Visto { get; set; }
    }
}
