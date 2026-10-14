namespace RR_Nueva_Naturaleza.Models.Dto
{
    // Se usa tanto para crear como para actualizar. Activo/eliminado se cambian con sus propias acciones auditadas.
    public class UmbralCultivoCreateDto
    {
        public Guid CultivoId { get; set; }
        public Guid SensorCultivoId { get; set; }
        // Null = todas las etapas
        public Guid? EtapaCultivoId { get; set; }
        public decimal? ValorMin { get; set; }
        public decimal? ValorMax { get; set; }
        public string NivelGravedad { get; set; } = string.Empty;
        public Guid? ActuadorCultivoId { get; set; }
        public bool? EncenderActuador { get; set; }
        public bool RevertirAlNormalizar { get; set; } = true;
        public int? MinutosEscalamientoSms { get; set; }
        public string? Descripcion { get; set; }

        // Motivo del cambio al modificar (opcional, queda en la auditoría)
        public string? Motivo { get; set; }
    }
}
