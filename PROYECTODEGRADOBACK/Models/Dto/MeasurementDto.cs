using System.ComponentModel.DataAnnotations.Schema;

namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class MeasurementDto
    {
        public Guid Id { get; set; }
        public decimal Value { get; set; }
        public string Date { get; set; }
        // foreign keys
        public Guid DeviceId { get; set; }
        public Guid Measurement_TypeId { get; set; }
    }
}
