using System.ComponentModel.DataAnnotations;

namespace RR_Nueva_Naturaleza.Models
{
    public class Rol
    {
        [Key]
        public Guid Id { get; set; }
        [MaxLength(20)]
        public string? Name { get; set; } = string.Empty;
    }
}
