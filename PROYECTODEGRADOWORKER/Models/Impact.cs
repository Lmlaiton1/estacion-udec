using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServicioSensorica.Models
{
    [Table("Impacts")] // <- nombre de la tabla en SQL Server. asegura que mapeamos a la tabla que ya existe en tu BD.
    public class Impact
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string Impact_Type { get; set; } = string.Empty;

    }
}
