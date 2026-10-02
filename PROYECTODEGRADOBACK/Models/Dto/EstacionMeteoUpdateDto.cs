namespace RR_Nueva_Naturaleza.Models.Dto
{
    // Sin Numero: es la clave alterna que usa la FK de Lecturas_Meteo, no se puede editar tras crearla.
    public class EstacionMeteoUpdateDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Ubicacion { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public bool Activa { get; set; } = true;
        public DateTime FechaInstalacion { get; set; }
    }
}
