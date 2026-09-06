using System.ComponentModel.DataAnnotations;

namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class Device_TypeDto
    {
        public Guid Id { get; set; }
        public string Device_Type_Name { get; set; } = string.Empty;
    }
}
