using System.ComponentModel.DataAnnotations;

namespace RR_Nueva_Naturaleza.Models
{
    // Traza de cambios del dominio meteo: activar/desactivar estaciones y sensores, CRUD de umbrales
    // y alertas marcadas como vistas. Tabla propia, no reutiliza Audits porque esa tabla exige un
    // DeviceId obligatorio (acuaponía).
    public class AuditoriaMeteo
    {
        [Key]
        public Guid Id { get; set; }

        [MaxLength(20)]
        public string TipoEntidad { get; set; } = string.Empty; // "Estacion" | "Sensor" | "Umbral" | "Alerta"

        public Guid EntidadId { get; set; }

        [MaxLength(100)]
        public string NombreEntidad { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Accion { get; set; } = string.Empty; // "Activada" | "Desactivada" | "Creado" | "Modificado" | "Eliminado" | "Vista"

        // Qué cambió (antes → después) o resumen de lo creado/eliminado. Null en activar/desactivar.
        [MaxLength(1000)]
        public string? Detalle { get; set; }

        [MaxLength(500)]
        public string Motivo { get; set; } = string.Empty;

        public DateTime Fecha { get; set; }

        // foreign keys
        public Guid UsuarioId { get; set; }
        public virtual User? Usuario { get; set; }
    }
}
