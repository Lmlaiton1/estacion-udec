using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServicioSensorica.Models
{
    // El worker genera estas filas automáticamente cuando una lectura viola un umbral activo.
    [Table("Alertas_Meteo")]
    public class AlertaMeteo
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid LecturaId { get; set; }

        [Required]
        public Guid UmbralId { get; set; }

        [Required]
        public int EstacionNumero { get; set; }

        [Required]
        [MaxLength(50)]
        public string Variable { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(10,4)")]
        public decimal ValorRegistrado { get; set; }

        [Required]
        [MaxLength(500)]
        public string Mensaje { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string NivelGravedad { get; set; } = string.Empty;

        [Required]
        public DateTime FechaHora { get; set; }

        public bool Visto { get; set; } = false;
    }
}
