using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RR_Nueva_Naturaleza.Models
{
    public class Measurement
    {
        [Key]
        public Guid Id { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal Value { get; set; }
        public DateTime Date { get; set; }
        // foreign keys
        public Guid DeviceId { get; set; }
        public virtual Device? Device { get; set; }
        public Guid Measurement_TypeId { get; set; }
        public virtual Measurement_Type? Measurement_Type { get; set; }
    }
}
