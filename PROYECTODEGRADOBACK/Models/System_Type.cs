using System.ComponentModel.DataAnnotations;

namespace RR_Nueva_Naturaleza.Models
{
    public class System_Type
    {
        [Key]
        public Guid Id { get; set; }
        [MaxLength(50)]
        public string System_Type_Name { get; set; } = string.Empty;
    }
}
