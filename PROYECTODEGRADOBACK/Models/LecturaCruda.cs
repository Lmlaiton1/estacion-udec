using System.ComponentModel.DataAnnotations;

namespace RR_Nueva_Naturaleza.Models
{
    public class LecturaCruda
    {
        [Key]
        public Guid Id { get; set; }

        public int EstacionNumero { get; set; }

        [MaxLength(100)]
        public string Topic { get; set; } = string.Empty;

        // JSON crudo publicado por la estación, validado con ISJSON en OnModelCreating
        public string Payload { get; set; } = string.Empty;

        public DateTime FechaRecepcion { get; set; }

        public bool Procesado { get; set; } = false;
    }
}
