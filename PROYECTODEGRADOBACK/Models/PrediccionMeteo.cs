using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RR_Nueva_Naturaleza.Models
{
    public class PrediccionMeteo
    {
        [Key]
        public Guid Id { get; set; }

        public int EstacionNumero { get; set; }

        [MaxLength(50)]
        public string Variable { get; set; } = string.Empty;

        public DateTime FechaPrediccion { get; set; }

        public int HorizonteHoras { get; set; }

        [Column(TypeName = "decimal(10,4)")]
        public decimal ValorPredicho { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal NivelConfianza { get; set; }

        [MaxLength(50)]
        public string ModeloVersion { get; set; } = string.Empty;

        public DateTime FechaGeneracion { get; set; }
    }
}
