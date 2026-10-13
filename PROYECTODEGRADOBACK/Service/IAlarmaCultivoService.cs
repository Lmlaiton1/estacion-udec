using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Models.Dto;

namespace RR_Nueva_Naturaleza.Service
{
    public interface IAlarmaCultivoService
    {
        Task<IEnumerable<AlarmaCultivo>> GetAlarmas(Guid? cultivoId, bool soloAbiertas, DateTime? desde, DateTime? hasta);
        Task<AlarmaCultivo?> GetAlarma(Guid id);
        Task<ServiceResponse> MarcarVista(Guid id, Guid usuarioId);
        Task<ServiceResponse> TomarControl(Guid id, string comentario, Guid usuarioId);
        Task<ServiceResponse> Comentar(Guid id, string comentario, Guid usuarioId);
        Task<ServiceResponse> Resolver(Guid id, string comentario, Guid usuarioId);

        // Administración (con motivo y auditoría)
        Task<ServiceResponse> UpdateAlarma(Guid id, AlarmaCultivoUpdateDto cambio, Guid usuarioId);
        Task<ServiceResponse> Anular(Guid id, string motivo, Guid usuarioId);
        Task<ServiceResponse> Reactivar(Guid id, string motivo, Guid usuarioId);
        Task<ServiceResponse> DeleteAlarma(Guid id, string motivo, Guid usuarioId);
    }
}
