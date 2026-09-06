using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RR_Nueva_Naturaleza.Models
{
    public class ChecklistHeader
    {
        [Key]
        public Guid Id { get; set; }

        public DateTime Fecha { get; set; }

        // FK hacia User
        public Guid UsuarioId { get; set; }
        public virtual User? Usuario { get; set; }

        // Observación general del checklist
        public string? Observacion { get; set; }

        // Relación con detalles (inicializada)
        public virtual ICollection<ChecklistDetail> Detalles { get; set; } = new List<ChecklistDetail>();
    }
}
