using System.ComponentModel.DataAnnotations;

namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class ImpactDto
    {
        public Guid Id { get; set; }
        public string Impact_Type { get; set; } = string.Empty;
    }
}
