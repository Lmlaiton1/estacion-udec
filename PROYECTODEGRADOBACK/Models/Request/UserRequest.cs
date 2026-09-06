namespace RR_Nueva_Naturaleza.Models.Request
{
    public class UserRequest
    {
        public Guid Id { get; set; }
        public string? Name { get; set; } = string.Empty;
        public string? Password { get; set; } = string.Empty;
        public Guid RolId { get; set; }
        public string? RolName { get; set; }
    }
}
