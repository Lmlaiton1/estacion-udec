namespace RR_Nueva_Naturaleza.Models.Dto
{
    // Qué se hizo al atender la alerta meteorológica (obligatorio, queda en la auditoría)
    public class MarcarVistaAlertaDto
    {
        public string AccionTomada { get; set; } = string.Empty;
    }
}
