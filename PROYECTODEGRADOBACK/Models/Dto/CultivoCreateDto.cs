namespace RR_Nueva_Naturaleza.Models.Dto
{
    // Se usa tanto para crear como para actualizar. Activo/eliminado se cambian con sus propias acciones auditadas.
    public class CultivoCreateDto
    {
        public int Numero { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Especie { get; set; } = string.Empty;
        public string? Variedad { get; set; }
        public string? Ubicacion { get; set; }
        public decimal? AreaM2 { get; set; }
        public DateTime? FechaSiembra { get; set; }
        // Números de las estaciones asociadas. Vacío = ninguna
        public List<int> Estaciones { get; set; } = new();
        public string? Descripcion { get; set; }

        // Motivo del cambio al modificar (opcional, queda en la auditoría)
        public string? Motivo { get; set; }
    }
}
