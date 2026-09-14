using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RR_Nueva_Naturaleza.Models
{
    public class UmbralMeteo
    {
        [Key]
        public Guid Id { get; set; }

        // Null = aplica a todas las estaciones
        public int? EstacionNumero { get; set; }

        [MaxLength(50)]
        public string Variable { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,4)")]
        public decimal? ValorMin { get; set; }

        [Column(TypeName = "decimal(10,4)")]
        public decimal? ValorMax { get; set; }

        [MaxLength(20)]
        public string NivelGravedad { get; set; } = string.Empty;

        public bool Activo { get; set; } = true;

        [MaxLength(200)]
        public string? Descripcion { get; set; }

        // foreign keys
        public Guid CreadoPor { get; set; }
        public virtual User? Usuario { get; set; }

        public DateTime FechaCreacion { get; set; }
    }
}
