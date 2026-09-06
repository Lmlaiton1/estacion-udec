using System.ComponentModel.DataAnnotations;

namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class System_TypeDto
    {
        public Guid Id { get; set; }
        public string System_Type_Name { get; set; } = string.Empty;
    }
}
