using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServicioSensorica.Models
{
    // Vista mínima de solo lectura del catálogo: el worker solo necesita validar que el sensor exista.
    [Table("Sensores_Meteo")]
    public class SensorMeteo
    {
        [Key]
        public Guid Id { get; set; }

        public bool Activo { get; set; }
    }
}
