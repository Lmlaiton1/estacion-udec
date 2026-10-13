namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class AuditoriaCultivoDto
    {
        public Guid Id { get; set; }
        public Guid? CultivoId { get; set; }
        public string TipoEntidad { get; set; } = string.Empty;
        public Guid EntidadId { get; set; }
        public string NombreEntidad { get; set; } = string.Empty;
        public string Accion { get; set; } = string.Empty;
        public string? Detalle { get; set; }
        public string? Motivo { get; set; }
        public string UsuarioNombre { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
    }
}
