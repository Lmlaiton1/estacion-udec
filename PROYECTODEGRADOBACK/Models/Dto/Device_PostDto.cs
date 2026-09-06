using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class Device_PostDto
    {
        public Guid Id { get; set; }
        public string Device_State { get; set; } = string.Empty;

    }
}
