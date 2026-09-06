using System.ComponentModel.DataAnnotations;

namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class Measurement_TypeDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        // foreign keys
        public Guid Unit_MeasurementId { get; set; }
    }
}
