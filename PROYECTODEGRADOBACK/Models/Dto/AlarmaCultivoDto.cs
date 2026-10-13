namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class AlarmaCultivoDto
    {
        public Guid Id { get; set; }
        public Guid CultivoId { get; set; }
        public string CultivoNombre { get; set; } = string.Empty;
        public Guid UmbralCultivoId { get; set; }
        public Guid SensorCultivoId { get; set; }
        public string? EtapaNombre { get; set; }
        public string Variable { get; set; } = string.Empty;
        public decimal ValorRegistrado { get; set; }
        public decimal UltimoValor { get; set; }
        public int Ocurrencias { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public string NivelGravedad { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaDisparo { get; set; }
        public DateTime FechaUltimaOcurrencia { get; set; }
        public string? VistaPorNombre { get; set; }
        public DateTime? FechaVista { get; set; }
        public string? AtendidaPorNombre { get; set; }
        public DateTime? FechaAtencion { get; set; }
        public string? ResueltaPorNombre { get; set; }
        public DateTime? FechaResolucion { get; set; }
        public string? Solucion { get; set; }
        public bool SmsEnviado { get; set; }
        public DateTime? FechaSms { get; set; }
        public List<BitacoraAlarmaCultivoDto> Bitacora { get; set; } = new();
    }
}
