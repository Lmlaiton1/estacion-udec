using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RR_Nueva_Naturaleza.Models
{
    // Raíz del módulo de control: sensores, actuadores, etapas y umbrales cuelgan de un cultivo.
    public class Cultivo
    {
        [Key]
        public Guid Id { get; set; }

        // Número único del cultivo, se usa en los topics MQTT: cultivo/{Numero}/datos
        public int Numero { get; set; }

        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Especie { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Variedad { get; set; }

        [MaxLength(200)]
        public string? Ubicacion { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? AreaM2 { get; set; }

        public DateTime? FechaSiembra { get; set; }

        [MaxLength(500)]
        public string? Descripcion { get; set; }

        public bool Activo { get; set; } = true;

        // Eliminación lógica: se oculta de las listas pero se conserva para la auditoría
        public bool Eliminado { get; set; } = false;

        // foreign keys
        public Guid CreadoPor { get; set; }
        public virtual User? Usuario { get; set; }

        public DateTime FechaCreacion { get; set; }

        public virtual ICollection<EtapaCultivo>? Etapas { get; set; }

        // Estaciones meteorológicas asociadas (una, varias o todas)
        public virtual ICollection<CultivoEstacion>? Estaciones { get; set; }
    }
}
