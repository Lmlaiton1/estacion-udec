using System.ComponentModel.DataAnnotations;

namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class RolDto
    {
        public Guid Id { get; set; }
        public string? Name { get; set; } = string.Empty;
    }
}
