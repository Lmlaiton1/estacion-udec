namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class ChecklistDetailDto
    {
        public Guid DispositivoId { get; set; }   
        public bool Estado { get; set; }          
        public Double? MedicionSensor { get; set; }   
        public Double? MedicionManual { get; set; }
    }
}
