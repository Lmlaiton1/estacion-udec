namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class BitacoraAlarmaCultivoDto
    {
        public Guid Id { get; set; }
        public Guid AlarmaCultivoId { get; set; }
        public string Accion { get; set; } = string.Empty;
        public string? Comentario { get; set; }
        // "Sistema" cuando no hay usuario
        public string UsuarioNombre { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
    }
}
