using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RR_Nueva_Naturaleza.Models
{
    // Resumen de una franja (Madrugada 0-5, Mañana 6-11, Tarde 12-17, Noche 18-23) del día pronosticado
    public class PronosticoFranjaMeteo
    {
        [Key]
        public Guid Id { get; set; }

        public Guid PronosticoId { get; set; }
        public virtual PronosticoMeteo? Pronostico { get; set; }

        [MaxLength(20)]
        public string Franja { get; set; } = string.Empty;

        public int Orden { get; set; }

        // Texto del modelo: Señal de sol, Posible nubosidad, Posible lluvia, Cielo no determinado, Condición variable
        [MaxLength(50)]
        public string CondicionPrevista { get; set; } = string.Empty;

        [Column(TypeName = "decimal(8,3)")]
        public decimal TemperaturaMedia { get; set; }

        [Column(TypeName = "decimal(8,3)")]
        public decimal TemperaturaMin { get; set; }

        [Column(TypeName = "decimal(8,3)")]
        public decimal TemperaturaMax { get; set; }

        [Column(TypeName = "decimal(8,3)")]
        public decimal HumedadMedia { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal LuzMediaLux { get; set; }

        [Column(TypeName = "decimal(8,3)")]
        public decimal LluviaMm { get; set; }

        public virtual RetroalimentacionMeteo? Retroalimentacion { get; set; }
    }
}
