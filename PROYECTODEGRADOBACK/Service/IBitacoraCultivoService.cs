using RR_Nueva_Naturaleza.Models;

namespace RR_Nueva_Naturaleza.Service
{
    public interface IBitacoraCultivoService
    {
        // Solo agregan al contexto: quien llama hace SaveChanges para que la acción y su auditoría
        // queden en la misma transacción.
        void AgregarEvento(Guid cultivoId, string tipoDispositivo, Guid dispositivoId, string nombreDispositivo,
            string accion, string origen, string motivo, Guid? usuarioId, Guid? alarmaId = null, Guid? umbralId = null);
        void AgregarBitacoraAlarma(Guid alarmaId, string accion, string? comentario, Guid? usuarioId);
        void AgregarAuditoria(Guid? cultivoId, string tipoEntidad, Guid entidadId, string nombreEntidad,
            string accion, string? detalle, string? motivo, Guid usuarioId);

        Task<IEnumerable<EventoDispositivoCultivo>> GetEventos(Guid? cultivoId, string? tipoDispositivo, DateTime? desde, DateTime? hasta);
        Task<IEnumerable<BitacoraAlarmaCultivo>> GetBitacoraAlarmas(Guid? cultivoId, DateTime? desde, DateTime? hasta);
        Task<IEnumerable<AuditoriaCultivo>> GetAuditoria(Guid? cultivoId, string? tipoEntidad, DateTime? desde, DateTime? hasta);
    }
}
