using System.ComponentModel.DataAnnotations;

namespace RR_Nueva_Naturaleza.Models
{
    // Auditoría de la configuración del control de cultivo: quién creó, modificó, eliminó,
    // desactivó o reactivó un cultivo, etapa, sensor, actuador, umbral o alarma, qué cambió y por qué.
    // Sin FK a la entidad: el rastro se conserva aunque la entidad se elimine.
    public class AuditoriaCultivo
    {
        [Key]
        public Guid Id { get; set; }

        public Guid? CultivoId { get; set; }

        // "Cultivo" | "Etapa" | "Sensor" | "Actuador" | "Umbral" | "Alarma"
        [MaxLength(20)]
        public string TipoEntidad { get; set; } = string.Empty;

        public Guid EntidadId { get; set; }

        [MaxLength(150)]
        public string NombreEntidad { get; set; } = string.Empty;

        // "Creado" | "Modificado" | "Eliminado" | "Activado" | "Desactivado" | "EtapaActual" | "Anulada" | "Reactivada"
        [MaxLength(30)]
        public string Accion { get; set; } = string.Empty;

        // Qué cambió: "Valor máximo: 30 → 32; Gravedad: Media → Alta"
        [MaxLength(2000)]
        public string? Detalle { get; set; }

        [MaxLength(500)]
        public string? Motivo { get; set; }

        public Guid UsuarioId { get; set; }
        public virtual User? Usuario { get; set; }

        public DateTime Fecha { get; set; }
    }

    public static class AccionesAuditoria
    {
        public const string Creado = "Creado";
        public const string Modificado = "Modificado";
        public const string Eliminado = "Eliminado";
        public const string Activado = "Activado";
        public const string Desactivado = "Desactivado";
        public const string EtapaActual = "EtapaActual";
        public const string QuitarEtapaActual = "QuitarEtapaActual";
        public const string Anulada = "Anulada";
        public const string Reactivada = "Reactivada";
    }
}
