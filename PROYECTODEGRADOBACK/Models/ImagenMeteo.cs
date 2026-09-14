using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RR_Nueva_Naturaleza.Models
{
    public class ImagenMeteo
    {
        [Key]
        public Guid Id { get; set; }

        public int EstacionNumero { get; set; }

        // Solo la ruta; la imagen queda en disco (datos/imagenes/estacionN/YYYY-MM-DD/)
        [MaxLength(500)]
        public string RutaArchivo { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Clasificacion { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? Confianza { get; set; }

        public DateTime FechaHora { get; set; }

        public bool Procesada { get; set; } = false;
    }
}
