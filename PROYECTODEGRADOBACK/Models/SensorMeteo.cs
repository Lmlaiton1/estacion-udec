using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RR_Nueva_Naturaleza.Models
{
    public class SensorMeteo
    {
        // Este Id es el GUID que envía el firmware en el JSON, no se genera en el backend
        [Key]
        public Guid Id { get; set; }

        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Tipo { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Variables { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Descripcion { get; set; }

        // Activo global: false = apagado en todas las estaciones
        public bool Activo { get; set; } = true;

        // Estaciones donde está desactivado puntualmente (Sensores_Meteo_Inactivos). Se llena en GetSensores.
        [NotMapped]
        public List<int> EstacionesInactivas { get; set; } = new();
    }
}
