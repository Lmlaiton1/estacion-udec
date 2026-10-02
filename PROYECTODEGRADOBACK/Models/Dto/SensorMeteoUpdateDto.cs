namespace RR_Nueva_Naturaleza.Models.Dto
{
    // Sin Id: es la clave primaria (el GUID del firmware) y la referencia la FK de Lecturas_Meteo, no se edita.
    public class SensorMeteoUpdateDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string Variables { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
    }
}
