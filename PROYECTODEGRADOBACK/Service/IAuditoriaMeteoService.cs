using RR_Nueva_Naturaleza.Models;

namespace RR_Nueva_Naturaleza.Service
{
    public interface IAuditoriaMeteoService
    {
        Task<IEnumerable<AuditoriaMeteo>> GetAuditoria();
        Task Registrar(string tipoEntidad, Guid entidadId, string nombreEntidad, bool nuevoEstado, string motivo, Guid usuarioId);
    }
}
