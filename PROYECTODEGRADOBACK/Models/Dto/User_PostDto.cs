namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class User_PostDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Last_Name { get; set; } = string.Empty;
        public string Id_Card { get; set; } = string.Empty;
        public string? Password { get; set; } = string.Empty;
        // foreign keys
        public string Rol { get; set; } = string.Empty;
    }
}
