namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class UmbralMeteoDto
    {
        public Guid Id { get; set; }
        public int? EstacionNumero { get; set; }
        public string Variable { get; set; } = string.Empty;
        public decimal? ValorMin { get; set; }
        public decimal? ValorMax { get; set; }
        public string NivelGravedad { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public string? Descripcion { get; set; }
        public Guid CreadoPor { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}
