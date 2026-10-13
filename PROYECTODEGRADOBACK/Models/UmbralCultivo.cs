using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RR_Nueva_Naturaleza.Models
{
    public class UmbralCultivo
    {
        [Key]
        public Guid Id { get; set; }

        // foreign keys
        public Guid CultivoId { get; set; }
        public virtual Cultivo? Cultivo { get; set; }

        public Guid SensorCultivoId { get; set; }
        public virtual SensorCultivo? Sensor { get; set; }

        // Null = aplica en todas las etapas
        public Guid? EtapaCultivoId { get; set; }
        public virtual EtapaCultivo? Etapa { get; set; }

        [Column(TypeName = "decimal(10,4)")]
        public decimal? ValorMin { get; set; }

        [Column(TypeName = "decimal(10,4)")]
        public decimal? ValorMax { get; set; }

        [MaxLength(20)]
        public string NivelGravedad { get; set; } = string.Empty;

        // Acción automática opcional al salir de rango, ej. encender el ventilador
        public Guid? ActuadorCultivoId { get; set; }
        public virtual ActuadorCultivo? Actuador { get; set; }

        // true = encender, false = apagar
        public bool? EncenderActuador { get; set; }

        // Cuando el valor vuelve a rango se revierte la acción automática
        public bool RevertirAlNormalizar { get; set; } = true;

        // Si nadie marca la alarma como vista en estos minutos se envía SMS. Null = sin SMS.
        public int? MinutosEscalamientoSms { get; set; }

        public bool Activo { get; set; } = true;

        // Eliminación lógica: se oculta de las listas pero se conserva para la auditoría
        public bool Eliminado { get; set; } = false;

        [MaxLength(200)]
        public string? Descripcion { get; set; }

        public Guid CreadoPor { get; set; }
        public virtual User? Usuario { get; set; }

        public DateTime FechaCreacion { get; set; }
    }
}
