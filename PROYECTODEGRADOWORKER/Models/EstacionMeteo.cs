using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServicioSensorica.Models
{
    // Vista mínima de solo lectura del catálogo: el worker solo necesita validar que la estación exista.
    [Table("Estaciones_Meteo")]
    public class EstacionMeteo
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public int Numero { get; set; }

        public bool Activa { get; set; }
    }
}
