using System.ComponentModel.DataAnnotations;

namespace RR_Nueva_Naturaleza.Models
{
    // Traza de activar/desactivar Estaciones_Meteo y Sensores_Meteo. Tabla propia del dominio meteo,
    // no reutiliza Audits porque esa tabla exige un DeviceId obligatorio (acuaponía).
    public class AuditoriaMeteo
    {
        [Key]
        public Guid Id { get; set; }

        [MaxLength(20)]
        public string TipoEntidad { get; set; } = string.Empty; // "Estacion" | "Sensor"

        public Guid EntidadId { get; set; }

        [MaxLength(100)]
        public string NombreEntidad { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Accion { get; set; } = string.Empty; // "Activada" | "Desactivada" | "Reiniciada"

        [MaxLength(500)]
        public string Motivo { get; set; } = string.Empty;

        public DateTime Fecha { get; set; }

        // foreign keys
        public Guid UsuarioId { get; set; }
        public virtual User? Usuario { get; set; }
    }
}
