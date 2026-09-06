using System.ComponentModel.DataAnnotations;

namespace RR_Nueva_Naturaleza.Models
{
    public class Impact
    {
        [Key]
        public Guid Id { get; set; }
        [MaxLength(10)]
        public string Impact_Type { get; set; } = string.Empty;
    }
}