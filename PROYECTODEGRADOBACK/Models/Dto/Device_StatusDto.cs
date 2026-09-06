using System.ComponentModel.DataAnnotations;

namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class Device_StatusDto
    {
        public Guid Id { get; set; }
        public string State_Name { get; set; } = string.Empty;
    }
}
