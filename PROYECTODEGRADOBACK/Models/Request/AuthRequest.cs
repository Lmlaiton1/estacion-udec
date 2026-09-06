using System.ComponentModel.DataAnnotations;
using System.Drawing;

namespace RR_Nueva_Naturaleza.Models.Request
{
    public class AuthRequest
    {
        public string user { get; set; } = string.Empty;
        public string? Password { get; set; } = string.Empty;

    }
}
