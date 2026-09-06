using System.ComponentModel.DataAnnotations;

namespace RR_Nueva_Naturaleza.Models
{
    public class User
    {
        [Key]
        public Guid Id { get; set; }
        [MaxLength(20)]
        public string Name { get; set; } = string.Empty;
        [MaxLength(20)]
        public string Last_Name { get; set; } = string.Empty;
        [MaxLength(20)]
        public string Id_Card { get; set; } = string.Empty;
        [MaxLength(256)]
        public string? Password { get; set; } = string.Empty;
        public int Questions_Type { get; set; } 
        public string? Answer { get; set; } = string.Empty;
        // foreign keys
        public Guid RolId { get; set; }
        public virtual Rol? Rol { get; set; }
    }
}
