namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class EtapaCultivoDto
    {
        public Guid Id { get; set; }
        public Guid CultivoId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int Orden { get; set; }
        public int? DuracionDias { get; set; }
        public DateTime? FechaInicio { get; set; }
        public bool Activa { get; set; }
        public string? Descripcion { get; set; }
    }
}
