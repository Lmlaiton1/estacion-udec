namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class SensorDataDto
    {
        public int Tipo_Com { get; set; }
        public int Dir_Esclavo { get; set; }
        public string Funcion { get; set; }
        public int Dir_Registro { get; set; }
        public int Sensorica { get; set; }
        public List<DatosSensor> datosSensores { get; set; }
    }

    public class DatosSensor
    {
        public float nL { get; set; }   // Nivel
        public float hA { get; set; }   // Humedad del aire
        public float tA { get; set; }   // Temperatura del aire
        public float tZ { get; set; }   // Turbidez
        public float tH { get; set; }   // Temperatura del agua
        public float TD { get; set; }   // Total de sólidos disueltos (TDS)
        public float pH { get; set; }   // pH del agua
        public float DO { get; set; }   // Oxígeno disuelto (DO)
    }
}
