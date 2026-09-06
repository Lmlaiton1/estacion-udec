using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServicioSensorica.Models
{
    [Table("System_Types")] // <- nombre de la tabla en SQL Server. asegura que mapeamos a la tabla que ya existe en tu BD.
    public class System_Type
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string System_Type_Name { get; set; } = string.Empty;
    }
}