namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class SensorCultivoDto
    {
        public Guid Id { get; set; }
        public Guid CultivoId { get; set; }
        public string CultivoNombre { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Clave { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string Variable { get; set; } = string.Empty;
        public string Unidad { get; set; } = string.Empty;
        public decimal? RangoMin { get; set; }
        public decimal? RangoMax { get; set; }
        public decimal? Precision { get; set; }
        public string? Marca { get; set; }
        public string? Especificaciones { get; set; }
        public bool Activo { get; set; }
        public Guid? DeviceId { get; set; }
        public DateTime FechaInstalacion { get; set; }
        public decimal? UltimoValor { get; set; }
        public DateTime? FechaUltimoValor { get; set; }
    }
}
