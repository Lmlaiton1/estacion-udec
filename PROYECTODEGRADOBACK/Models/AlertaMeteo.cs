using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RR_Nueva_Naturaleza.Models
{
    public class AlertaMeteo
    {
        [Key]
        public Guid Id { get; set; }

        // foreign keys
        public Guid LecturaId { get; set; }
        public virtual LecturaMeteo? Lectura { get; set; }

        public Guid UmbralId { get; set; }
        public virtual UmbralMeteo? Umbral { get; set; }

        public int EstacionNumero { get; set; }

        [MaxLength(50)]
        public string Variable { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,4)")]
        public decimal ValorRegistrado { get; set; }

        [MaxLength(500)]
        public string Mensaje { get; set; } = string.Empty;

        [MaxLength(20)]
        public string NivelGravedad { get; set; } = string.Empty;

        public DateTime FechaHora { get; set; }

        public bool Visto { get; set; } = false;
    }
}
