namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class CambiarModoActuadorDto
    {
        // "Automatico" | "Manual"
        public string Modo { get; set; } = string.Empty;
        public string Motivo { get; set; } = string.Empty;
    }
}
