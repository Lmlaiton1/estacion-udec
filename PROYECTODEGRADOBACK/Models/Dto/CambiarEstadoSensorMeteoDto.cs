namespace RR_Nueva_Naturaleza.Models.Dto
{
    // Activar/desactivar un SensorMeteo. EstacionNumero null = en todas las estaciones.
    public class CambiarEstadoSensorMeteoDto
    {
        public bool Activo { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public int? EstacionNumero { get; set; }
    }
}
