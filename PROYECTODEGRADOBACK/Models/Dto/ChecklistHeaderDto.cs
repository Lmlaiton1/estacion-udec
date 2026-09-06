namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class ChecklistHeaderDto
    {
        public Guid UsuarioId { get; set; }  
        public DateTime Fecha { get; set; }     
        public string? Observacion { get; set; }     
        public List<ChecklistDetailDto> Detalles { get; set; } = new List<ChecklistDetailDto>();
    }
}
