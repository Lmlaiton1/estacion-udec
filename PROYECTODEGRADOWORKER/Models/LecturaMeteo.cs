using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServicioSensorica.Models
{
    // Una fila por variable meteorológica (temperatura, humedad, radiacion, velocidadViento, direccion, lluvia)
    [Table("Lecturas_Meteo")]
    public class LecturaMeteo
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public int EstacionNumero { get; set; }

        [Required]
        public Guid SensorId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Variable { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(10,4)")]
        public decimal Valor { get; set; }

        [MaxLength(10)]
        public string? ValorCardinal { get; set; }

        [Required]
        [MaxLength(20)]
        public string Unidad { get; set; } = string.Empty;

        [Required]
        public DateTime FechaHora { get; set; }

        [Required]
        public DateTime FechaRecepcion { get; set; }

        public int? PulsosRaw { get; set; }

        public int? IntervaloMs { get; set; }
    }
}
