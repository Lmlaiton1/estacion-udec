namespace RR_Nueva_Naturaleza.Models.Dto
{
    // Usado tanto para activar/desactivar una EstacionMeteo como un SensorMeteo.
    public class CambiarEstadoDto
    {
        public bool Activo { get; set; }
        public string Motivo { get; set; } = string.Empty;
    }
}
