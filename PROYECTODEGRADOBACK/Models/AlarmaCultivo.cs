using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RR_Nueva_Naturaleza.Models
{
    // Una alarma abierta por umbral: mientras siga sin resolver, las nuevas lecturas fuera de rango
    // suman ocurrencias en vez de crear filas nuevas. El detalle de quién hizo qué va en BitacoraAlarmaCultivo.
    public class AlarmaCultivo
    {
        [Key]
        public Guid Id { get; set; }

        // foreign keys
        public Guid CultivoId { get; set; }
        public virtual Cultivo? Cultivo { get; set; }

        public Guid UmbralCultivoId { get; set; }
        public virtual UmbralCultivo? Umbral { get; set; }

        public Guid SensorCultivoId { get; set; }

        [MaxLength(100)]
        public string? EtapaNombre { get; set; }

        [MaxLength(50)]
        public string Variable { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,4)")]
        public decimal ValorRegistrado { get; set; }

        [Column(TypeName = "decimal(10,4)")]
        public decimal UltimoValor { get; set; }

        public int Ocurrencias { get; set; } = 1;

        [MaxLength(500)]
        public string Mensaje { get; set; } = string.Empty;

        [MaxLength(20)]
        public string NivelGravedad { get; set; } = string.Empty;

        // Activa -> Vista -> EnAtencion -> Resuelta. "Anulada" = descartada por un administrador (falsa alarma)
        [MaxLength(20)]
        public string Estado { get; set; } = EstadosAlarma.Activa;

        public DateTime FechaDisparo { get; set; }
        public DateTime FechaUltimaOcurrencia { get; set; }

        public Guid? VistaPorId { get; set; }
        public virtual User? VistaPor { get; set; }
        public DateTime? FechaVista { get; set; }

        public Guid? AtendidaPorId { get; set; }
        public virtual User? AtendidaPor { get; set; }
        public DateTime? FechaAtencion { get; set; }

        public Guid? ResueltaPorId { get; set; }
        public virtual User? ResueltaPor { get; set; }
        public DateTime? FechaResolucion { get; set; }

        [MaxLength(1000)]
        public string? Solucion { get; set; }

        public bool SmsEnviado { get; set; } = false;
        public DateTime? FechaSms { get; set; }

        // Eliminación lógica: se oculta de las listas pero se conserva para la auditoría
        public bool Eliminado { get; set; } = false;

        public virtual ICollection<BitacoraAlarmaCultivo>? Bitacora { get; set; }
    }

    public static class EstadosAlarma
    {
        public const string Activa = "Activa";
        public const string Vista = "Vista";
        public const string EnAtencion = "EnAtencion";
        public const string Resuelta = "Resuelta";
        public const string Anulada = "Anulada";
    }
}
