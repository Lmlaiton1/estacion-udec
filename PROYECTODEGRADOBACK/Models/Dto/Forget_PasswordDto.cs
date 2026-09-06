using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Globalization;

namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class Forget_PasswordDto
    {
        public string user { get; set; } = string.Empty;
        public int question_type { get; set; }
        public string answer { get; set; }
        public string newPassword { get; set; }
    }
    
}
