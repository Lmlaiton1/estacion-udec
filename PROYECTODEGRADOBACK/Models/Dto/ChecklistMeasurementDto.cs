namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class ChecklistMeasurementDto
    {
        public DateTime Fecha { get; set; }
        public decimal? MedicionSensor { get; set; }
        public decimal? MedicionManual { get; set; }
        public string SensorNombre { get; set; } = "";
    }
}
