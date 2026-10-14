namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class UmbralCultivoDto
    {
        public Guid Id { get; set; }
        public Guid CultivoId { get; set; }
        public string CultivoNombre { get; set; } = string.Empty;
        public Guid SensorCultivoId { get; set; }
        public string SensorNombre { get; set; } = string.Empty;
        public string Variable { get; set; } = string.Empty;
        public string Unidad { get; set; } = string.Empty;
        public Guid? EtapaCultivoId { get; set; }
        public string? EtapaNombre { get; set; }
        public decimal? ValorMin { get; set; }
        public decimal? ValorMax { get; set; }
        public string NivelGravedad { get; set; } = string.Empty;
        public Guid? ActuadorCultivoId { get; set; }
        public string? ActuadorNombre { get; set; }
        public bool? EncenderActuador { get; set; }
        public bool RevertirAlNormalizar { get; set; }
        public int? MinutosEscalamientoSms { get; set; }
        public bool Activo { get; set; }
        public string? Descripcion { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}
