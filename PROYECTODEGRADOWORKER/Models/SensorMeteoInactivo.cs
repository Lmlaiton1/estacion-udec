using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServicioSensorica.Models
{
    // Vista de solo lectura: sensor desactivado solo en una estación (lo administra el backend).
    [Table("Sensores_Meteo_Inactivos")]
    public class SensorMeteoInactivo
    {
        public Guid SensorId { get; set; }

        public int EstacionNumero { get; set; }
    }
}
