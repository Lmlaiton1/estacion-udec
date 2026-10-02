namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class EstacionMeteoCreateDto
    {
        public int Numero { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Ubicacion { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public bool Activa { get; set; } = true;
        public DateTime FechaInstalacion { get; set; }
    }
}
