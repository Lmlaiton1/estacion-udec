using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServicioSensorica.Models
{
    // Vistas mínimas de las tablas del control de cultivo. El esquema lo administra el backend
    // (migración AddControlCultivo); acá solo están las columnas que el worker lee o escribe.

    [Table("Cultivos")]
    public class CultivoW
    {
        [Key]
        public Guid Id { get; set; }
        public int Numero { get; set; }
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }

    [Table("Etapas_Cultivo")]
    public class EtapaCultivoW
    {
        [Key]
        public Guid Id { get; set; }
        public Guid CultivoId { get; set; }
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;
        public bool Activa { get; set; }
    }

    [Table("Sensores_Cultivo")]
    public class SensorCultivoW
    {
        [Key]
        public Guid Id { get; set; }
        public Guid CultivoId { get; set; }
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;
        [MaxLength(50)]
        public string Clave { get; set; } = string.Empty;
        [MaxLength(50)]
        public string Variable { get; set; } = string.Empty;
        [MaxLength(20)]
        public string Unidad { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }

    [Table("Actuadores_Cultivo")]
    public class ActuadorCultivoW
    {
        [Key]
        public Guid Id { get; set; }
        public Guid CultivoId { get; set; }
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;
        [MaxLength(50)]
        public string Clave { get; set; } = string.Empty;
        [MaxLength(20)]
        public string Modo { get; set; } = string.Empty;
        public bool Encendido { get; set; }
        public bool Activo { get; set; }
        public DateTime? FechaUltimoCambio { get; set; }
    }

    [Table("Umbrales_Cultivo")]
    public class UmbralCultivoW
    {
        [Key]
        public Guid Id { get; set; }
        public Guid CultivoId { get; set; }
        public Guid SensorCultivoId { get; set; }
        public Guid? EtapaCultivoId { get; set; }
        [Column(TypeName = "decimal(10,4)")]
        public decimal? ValorMin { get; set; }
        [Column(TypeName = "decimal(10,4)")]
        public decimal? ValorMax { get; set; }
        [MaxLength(20)]
        public string NivelGravedad { get; set; } = string.Empty;
        public Guid? ActuadorCultivoId { get; set; }
        public bool? EncenderActuador { get; set; }
        public bool RevertirAlNormalizar { get; set; }
        public int? MinutosEscalamientoSms { get; set; }
        public bool Activo { get; set; }
    }

    [Table("Lecturas_Cultivo")]
    public class LecturaCultivoW
    {
        [Key]
        public Guid Id { get; set; }
        public Guid CultivoId { get; set; }
        public Guid SensorCultivoId { get; set; }
        [MaxLength(50)]
        public string Variable { get; set; } = string.Empty;
        [Column(TypeName = "decimal(10,4)")]
        public decimal Valor { get; set; }
        [MaxLength(20)]
        public string Unidad { get; set; } = string.Empty;
        public DateTime FechaHora { get; set; }
    }

    [Table("Alarmas_Cultivo")]
    public class AlarmaCultivoW
    {
        [Key]
        public Guid Id { get; set; }
        public Guid CultivoId { get; set; }
        public Guid UmbralCultivoId { get; set; }
        public Guid SensorCultivoId { get; set; }
        [MaxLength(100)]
        public string? EtapaNombre { get; set; }
        [MaxLength(50)]
        public string Variable { get; set; } = string.Empty;
        [Column(TypeName = "decimal(10,4)")]
        public decimal ValorRegistrado { get; set; }
        [Column(TypeName = "decimal(10,4)")]
        public decimal UltimoValor { get; set; }
        public int Ocurrencias { get; set; }
        [MaxLength(500)]
        public string Mensaje { get; set; } = string.Empty;
        [MaxLength(20)]
        public string NivelGravedad { get; set; } = string.Empty;
        [MaxLength(20)]
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaDisparo { get; set; }
        public DateTime FechaUltimaOcurrencia { get; set; }
        public bool SmsEnviado { get; set; }
        public DateTime? FechaSms { get; set; }
        public bool Eliminado { get; set; }
    }

    [Table("Bitacora_Alarmas_Cultivo")]
    public class BitacoraAlarmaCultivoW
    {
        [Key]
        public Guid Id { get; set; }
        public Guid AlarmaCultivoId { get; set; }
        [MaxLength(30)]
        public string Accion { get; set; } = string.Empty;
        [MaxLength(1000)]
        public string? Comentario { get; set; }
        public Guid? UsuarioId { get; set; }
        public DateTime Fecha { get; set; }
    }

    [Table("Eventos_Dispositivo_Cultivo")]
    public class EventoDispositivoCultivoW
    {
        [Key]
        public Guid Id { get; set; }
        public Guid CultivoId { get; set; }
        [MaxLength(20)]
        public string TipoDispositivo { get; set; } = string.Empty;
        public Guid DispositivoId { get; set; }
        [MaxLength(100)]
        public string NombreDispositivo { get; set; } = string.Empty;
        [MaxLength(30)]
        public string Accion { get; set; } = string.Empty;
        [MaxLength(20)]
        public string Origen { get; set; } = string.Empty;
        [MaxLength(500)]
        public string Motivo { get; set; } = string.Empty;
        public Guid? UsuarioId { get; set; }
        public Guid? AlarmaCultivoId { get; set; }
        public Guid? UmbralCultivoId { get; set; }
        public DateTime Fecha { get; set; }
    }
}
