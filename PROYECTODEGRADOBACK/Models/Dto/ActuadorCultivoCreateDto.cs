namespace RR_Nueva_Naturaleza.Models.Dto
{
    // Se usa tanto para crear como para actualizar. Encendido, modo y activo se cambian con
    // sus propios endpoints para que queden auditados.
    public class ActuadorCultivoCreateDto
    {
        public Guid CultivoId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Clave { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string? Marca { get; set; }
        public string? Potencia { get; set; }
        public string? Especificaciones { get; set; }

        // Motivo del cambio al modificar (opcional, queda en la auditoría)
        public string? Motivo { get; set; }
    }
}
