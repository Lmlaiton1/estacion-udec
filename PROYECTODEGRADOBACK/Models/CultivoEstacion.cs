namespace RR_Nueva_Naturaleza.Models
{
    // Estaciones meteorológicas que vigilan un cultivo. Un cultivo puede usar una, varias o todas,
    // y una estación puede servir a varios cultivos.
    public class CultivoEstacion
    {
        public Guid CultivoId { get; set; }
        public virtual Cultivo? Cultivo { get; set; }

        // FK contra Estaciones_Meteo.Numero (clave alterna)
        public int EstacionNumero { get; set; }
    }
}
