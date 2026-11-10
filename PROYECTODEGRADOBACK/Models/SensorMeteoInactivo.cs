namespace RR_Nueva_Naturaleza.Models
{
    // Sensor desactivado solo en una estación. Las 3 estaciones comparten el mismo GUID de sensor,
    // así que el Activo global de SensorMeteo no sirve para apagar el DHT22 de una sola estación.
    // Un sensor está activo en la estación N si SensorMeteo.Activo y no existe fila (SensorId, N) acá.
    public class SensorMeteoInactivo
    {
        public Guid SensorId { get; set; }

        public int EstacionNumero { get; set; }
    }
}
