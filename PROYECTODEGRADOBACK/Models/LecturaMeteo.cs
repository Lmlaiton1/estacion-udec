using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RR_Nueva_Naturaleza.Models
{
    public class LecturaMeteo
    {
        [Key]
        public Guid Id { get; set; }

        // Discriminante de estación: se identifica por número de topic, no por el GUID del sensor
        public int EstacionNumero { get; set; }

        public Guid SensorId { get; set; }

        [MaxLength(50)]
        public string Variable { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,4)")]
        public decimal Valor { get; set; }

        [MaxLength(10)]
        public string? ValorCardinal { get; set; }

        [MaxLength(20)]
        public string Unidad { get; set; } = string.Empty;

        public DateTime FechaHora { get; set; }

        public DateTime FechaRecepcion { get; set; }

        // Solo aplica a variables por pulsos (viento, lluvia); sin antirrebote en el firmware aún
        public int? PulsosRaw { get; set; }

        public int? IntervaloMs { get; set; }
    }
}
