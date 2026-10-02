namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class SensorMeteoCreateDto
    {
        // Debe ser el mismo GUID que ya usa el firmware/worker para este sensor, no se autogenera.
        public Guid Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string Variables { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
    }
}
