using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RR_Nueva_Naturaleza.Models
{
    public class PrediccionMeteo
    {
        [Key]
        public Guid Id { get; set; }

        // Ejecución del pronóstico diario a la que pertenece (null en registros anteriores)
        public Guid? PronosticoId { get; set; }
        public virtual PronosticoMeteo? Pronostico { get; set; }

        public int EstacionNumero { get; set; }

        [MaxLength(50)]
        public string Variable { get; set; } = string.Empty;

        public DateTime FechaPrediccion { get; set; }

        public int HorizonteHoras { get; set; }

        [Column(TypeName = "decimal(10,4)")]
        public decimal ValorPredicho { get; set; }

        // Banda de error: percentil 90 de residuos de validación (no garantiza cobertura del 90 %)
        [Column(TypeName = "decimal(10,4)")]
        public decimal? LimiteInferior { get; set; }

        [Column(TypeName = "decimal(10,4)")]
        public decimal? LimiteSuperior { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal NivelConfianza { get; set; }

        [MaxLength(50)]
        public string ModeloVersion { get; set; } = string.Empty;

        public DateTime FechaGeneracion { get; set; }
    }
}
