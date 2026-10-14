using System.ComponentModel.DataAnnotations;

namespace RR_Nueva_Naturaleza.Models
{
    // Auditoría de sensores y actuadores: qué pasó, si fue automático o manual, quién y para qué.
    public class EventoDispositivoCultivo
    {
        [Key]
        public Guid Id { get; set; }

        public Guid CultivoId { get; set; }

        // "Sensor" | "Actuador"
        [MaxLength(20)]
        public string TipoDispositivo { get; set; } = string.Empty;

        public Guid DispositivoId { get; set; }

        [MaxLength(100)]
        public string NombreDispositivo { get; set; } = string.Empty;

        // "Encendido" | "Apagado" | "Activado" | "Desactivado" | "ModoAutomatico" | "ModoManual"
        [MaxLength(30)]
        public string Accion { get; set; } = string.Empty;

        // "Automatico" | "Manual"
        [MaxLength(20)]
        public string Origen { get; set; } = string.Empty;

        // Para qué se hizo
        [MaxLength(500)]
        public string Motivo { get; set; } = string.Empty;

        // Null cuando lo hizo el sistema
        public Guid? UsuarioId { get; set; }
        public virtual User? Usuario { get; set; }

        // Alarma o umbral que lo provocó, si aplica
        public Guid? AlarmaCultivoId { get; set; }
        public Guid? UmbralCultivoId { get; set; }

        public DateTime Fecha { get; set; }
    }
}
