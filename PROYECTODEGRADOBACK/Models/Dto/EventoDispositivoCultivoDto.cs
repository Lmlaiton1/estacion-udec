namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class EventoDispositivoCultivoDto
    {
        public Guid Id { get; set; }
        public Guid CultivoId { get; set; }
        public string TipoDispositivo { get; set; } = string.Empty;
        public Guid DispositivoId { get; set; }
        public string NombreDispositivo { get; set; } = string.Empty;
        public string Accion { get; set; } = string.Empty;
        public string Origen { get; set; } = string.Empty;
        public string Motivo { get; set; } = string.Empty;
        // "Sistema" cuando fue automático
        public string UsuarioNombre { get; set; } = string.Empty;
        public Guid? AlarmaCultivoId { get; set; }
        public Guid? UmbralCultivoId { get; set; }
        public DateTime Fecha { get; set; }
    }
}
