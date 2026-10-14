using System.ComponentModel.DataAnnotations;

namespace RR_Nueva_Naturaleza.Models
{
    // Auditoría de cada paso de una alarma: quién la vio, quién tomó el control, cómo se resolvió.
    public class BitacoraAlarmaCultivo
    {
        [Key]
        public Guid Id { get; set; }

        // foreign keys
        public Guid AlarmaCultivoId { get; set; }
        public virtual AlarmaCultivo? Alarma { get; set; }

        // "Disparada" | "Vista" | "TomoControl" | "Comentario" | "Resuelta" | "SmsEnviado" | "AccionAutomatica"
        [MaxLength(30)]
        public string Accion { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Comentario { get; set; }

        // Null = acción del sistema
        public Guid? UsuarioId { get; set; }
        public virtual User? Usuario { get; set; }

        public DateTime Fecha { get; set; }
    }
}
