using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RR_Nueva_Naturaleza.Models
{
    // Una ejecución del botón "Pronóstico del día": reentrenamiento + pronóstico del día siguiente.
    // Los valores horarios (48 h) quedan en Predicciones_Meteo y el resumen por franja en Pronosticos_Franja_Meteo.
    public class PronosticoMeteo
    {
        [Key]
        public Guid Id { get; set; }

        public DateTime FechaGeneracion { get; set; }

        public Guid GeneradoPorId { get; set; }
        public virtual User? GeneradoPor { get; set; }

        [Column(TypeName = "date")]
        public DateTime DiaPronosticado { get; set; }

        // Última hora observada de la estación 3 usada como entrada (ventana de 24 h)
        public DateTime OrigenDatos { get; set; }

        [MaxLength(50)]
        public string ModeloVersion { get; set; } = string.Empty;

        // Umbrales de cielo usados en esta emisión (calibrados con la retroalimentación)
        [Column(TypeName = "decimal(6,3)")]
        public decimal UmbralLluvia { get; set; }

        [Column(TypeName = "decimal(6,3)")]
        public decimal FactorSol { get; set; }

        [MaxLength(300)]
        public string? Advertencia { get; set; }

        public int VentanasEntrenamiento { get; set; }

        [Column(TypeName = "decimal(8,2)")]
        public decimal SegundosEjecucion { get; set; }

        public virtual ICollection<PronosticoFranjaMeteo> Franjas { get; set; } = new List<PronosticoFranjaMeteo>();
    }
}
