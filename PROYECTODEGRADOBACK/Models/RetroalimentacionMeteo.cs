using System.ComponentModel.DataAnnotations;

namespace RR_Nueva_Naturaleza.Models
{
    // Lo que el usuario observó en una franja pronosticada. Se usa para calibrar los umbrales del cielo.
    public class RetroalimentacionMeteo
    {
        [Key]
        public Guid Id { get; set; }

        // Una sola calificación por franja (índice único)
        public Guid FranjaId { get; set; }
        public virtual PronosticoFranjaMeteo? Franja { get; set; }

        // Soleado, Nublado o Lluvioso
        [MaxLength(20)]
        public string CondicionReal { get; set; } = string.Empty;

        public bool Acerto { get; set; }

        public Guid UsuarioId { get; set; }
        public virtual User? Usuario { get; set; }

        public DateTime Fecha { get; set; }
    }
}
