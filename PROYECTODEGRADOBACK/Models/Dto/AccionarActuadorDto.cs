namespace RR_Nueva_Naturaleza.Models.Dto
{
    // Encendido/apagado manual. El motivo ("para qué") es obligatorio.
    public class AccionarActuadorDto
    {
        public bool Encender { get; set; }
        public string Motivo { get; set; } = string.Empty;

        // Alarma que se está atendiendo con esta acción, si aplica
        public Guid? AlarmaCultivoId { get; set; }
    }
}
