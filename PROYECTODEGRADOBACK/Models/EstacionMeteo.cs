using System.ComponentModel.DataAnnotations;

namespace RR_Nueva_Naturaleza.Models
{
    public class EstacionMeteo
    {
        [Key]
        public Guid Id { get; set; }

        public int Numero { get; set; }

        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Ubicacion { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Descripcion { get; set; }

        public bool Activa { get; set; } = true;

        public DateTime FechaInstalacion { get; set; }
    }
}
