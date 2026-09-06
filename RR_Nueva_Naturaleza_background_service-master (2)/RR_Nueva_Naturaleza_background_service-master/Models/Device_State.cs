using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServicioSensorica.Models
{
    [Table("Device_States")] // <- nombre de la tabla en SQL Server. asegura que mapeamos a la tabla que ya existe en tu BD.
    public class Device_State
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string State_Name { get; set; } = string.Empty;

    }
}
