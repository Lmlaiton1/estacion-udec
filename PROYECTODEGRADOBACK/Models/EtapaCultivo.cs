using System.ComponentModel.DataAnnotations;

namespace RR_Nueva_Naturaleza.Models
{
    // Etapa productiva (germinación, vegetativo, floración...). Solo una activa por cultivo.
    public class EtapaCultivo
    {
        [Key]
        public Guid Id { get; set; }

        // foreign keys
        public Guid CultivoId { get; set; }
        public virtual Cultivo? Cultivo { get; set; }

        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        public int Orden { get; set; }

        public int? DuracionDias { get; set; }

        public DateTime? FechaInicio { get; set; }

        public bool Activa { get; set; } = false;

        // Eliminación lógica: se oculta de las listas pero se conserva para la auditoría
        public bool Eliminado { get; set; } = false;

        [MaxLength(500)]
        public string? Descripcion { get; set; }
    }
}
