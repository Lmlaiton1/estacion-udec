using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace RR_Nueva_Naturaleza.Models
{
    public class ChecklistDetail
    {
        [Key]
        public Guid Id { get; set; }

        // Relación con cabecera
        public Guid ChecklistHeaderId { get; set; }
        public virtual ChecklistHeader? ChecklistHeader { get; set; }

        // Relación con dispositivo
        public Guid DispositivoId { get; set; }
        public virtual Device? Dispositivo { get; set; }

        [MaxLength(50)]
        public string? Estado { get; set; }  // "Encendido", "Apagado", "N/A"

        // Valores ingresados manualmente
        public double? MedicionSensor { get; set; }
        public double? MedicionManual { get; set; }
    }

}