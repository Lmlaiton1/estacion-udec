namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class CultivoDto
    {
        public Guid Id { get; set; }
        public int Numero { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Especie { get; set; } = string.Empty;
        public string? Variedad { get; set; }
        public string? Ubicacion { get; set; }
        public decimal? AreaM2 { get; set; }
        public DateTime? FechaSiembra { get; set; }
        // Números de las estaciones asociadas. Vacío = ninguna
        public List<int> Estaciones { get; set; } = new();
        public string? Descripcion { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string? EtapaActual { get; set; }
        public List<EtapaCultivoDto> Etapas { get; set; } = new();
    }
}
