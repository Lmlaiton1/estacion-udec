using System.ComponentModel.DataAnnotations;

namespace RR_Nueva_Naturaleza.Models
{
    public class Measurement_Type
    {
        [Key]
        public Guid Id { get; set; }
        [MaxLength(20)]
        public string Name { get; set; } = string.Empty;
        // foreign keys
        public Guid Unit_MeasurementId { get; set; }
        public virtual Unit_Measurement? Unit_Measurement { get; set; }
    }
}