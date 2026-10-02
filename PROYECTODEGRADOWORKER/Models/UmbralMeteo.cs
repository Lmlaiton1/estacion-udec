using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServicioSensorica.Models
{
    // Vista mínima de solo lectura: el worker solo necesita evaluar lecturas contra umbrales activos.
    [Table("Umbrales_Meteo")]
    public class UmbralMeteo
    {
        [Key]
        public Guid Id { get; set; }

        // Null = aplica a las 3 estaciones
        public int? EstacionNumero { get; set; }

        [Required]
        [MaxLength(50)]
        public string Variable { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,4)")]
        public decimal? ValorMin { get; set; }

        [Column(TypeName = "decimal(10,4)")]
        public decimal? ValorMax { get; set; }

        [Required]
        [MaxLength(20)]
        public string NivelGravedad { get; set; } = string.Empty;

        public bool Activo { get; set; } = true;
    }
}
