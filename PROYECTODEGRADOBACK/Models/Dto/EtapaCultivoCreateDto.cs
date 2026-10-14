namespace RR_Nueva_Naturaleza.Models.Dto
{
    // Se usa tanto para crear como para actualizar
    public class EtapaCultivoCreateDto
    {
        public Guid CultivoId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int Orden { get; set; }
        public int? DuracionDias { get; set; }
        public DateTime? FechaInicio { get; set; }
        public string? Descripcion { get; set; }

        // Motivo del cambio al modificar (opcional, queda en la auditoría)
        public string? Motivo { get; set; }
    }
}
