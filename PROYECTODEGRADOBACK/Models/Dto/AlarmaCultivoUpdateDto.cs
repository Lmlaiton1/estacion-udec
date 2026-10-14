namespace RR_Nueva_Naturaleza.Models.Dto
{
    // Corrección de una alarma (ej. reclasificar la gravedad). El motivo es obligatorio.
    public class AlarmaCultivoUpdateDto
    {
        public string NivelGravedad { get; set; } = string.Empty;
        // Solo aplica si la alarma ya está resuelta
        public string? Solucion { get; set; }
        public string Motivo { get; set; } = string.Empty;
    }
}
