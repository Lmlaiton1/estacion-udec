using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RR_Nueva_Naturaleza.Models
{
    // La escribe el worker al recibir cultivo/{Numero}/datos
    public class LecturaCultivo
    {
        [Key]
        public Guid Id { get; set; }

        public Guid CultivoId { get; set; }

        public Guid SensorCultivoId { get; set; }

        [MaxLength(50)]
        public string Variable { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,4)")]
        public decimal Valor { get; set; }

        [MaxLength(20)]
        public string Unidad { get; set; } = string.Empty;

        public DateTime FechaHora { get; set; }
    }
}
