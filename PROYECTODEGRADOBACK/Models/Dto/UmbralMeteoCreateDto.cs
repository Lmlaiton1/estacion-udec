namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class UmbralMeteoCreateDto
    {
        // Null = aplica a las 3 estaciones
        public int? EstacionNumero { get; set; }
        public string Variable { get; set; } = string.Empty;
        public decimal? ValorMin { get; set; }
        public decimal? ValorMax { get; set; }
        public string NivelGravedad { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;
        public string? Descripcion { get; set; }
        // Obligatorio: queda en la auditoría
        public string Motivo { get; set; } = string.Empty;
    }
}
