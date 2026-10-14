using RR_Nueva_Naturaleza.Models;

namespace RR_Nueva_Naturaleza.Service
{
    public interface IAuditoriaMeteoService
    {
        Task<IEnumerable<AuditoriaMeteo>> GetAuditoria();
        Task Registrar(string tipoEntidad, Guid entidadId, string nombreEntidad, bool nuevoEstado, string motivo, Guid usuarioId);

        // Agrega el registro al contexto sin guardar: se persiste en el mismo SaveChanges que el cambio auditado
        void Agregar(string tipoEntidad, Guid entidadId, string nombreEntidad, string accion, string? detalle, string motivo, Guid usuarioId);
    }
}
