namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class ActuadorCultivoDto
    {
        public Guid Id { get; set; }
        public Guid CultivoId { get; set; }
        public string CultivoNombre { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Clave { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string? Marca { get; set; }
        public string? Potencia { get; set; }
        public string? Especificaciones { get; set; }
        public string Modo { get; set; } = string.Empty;
        public bool Encendido { get; set; }
        public bool Activo { get; set; }
        public Guid? DeviceId { get; set; }
        public DateTime? FechaUltimoCambio { get; set; }
    }
}
