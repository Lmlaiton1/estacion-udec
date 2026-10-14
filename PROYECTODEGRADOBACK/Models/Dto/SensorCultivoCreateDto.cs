namespace RR_Nueva_Naturaleza.Models.Dto
{
    // Se usa tanto para crear como para actualizar. El estado activo se cambia con CambiarEstado (queda auditado).
    public class SensorCultivoCreateDto
    {
        public Guid CultivoId { get; set; }
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
        public DateTime? FechaInstalacion { get; set; }

        // Motivo del cambio al modificar (opcional, queda en la auditoría)
        public string? Motivo { get; set; }
    }
}
