using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Models.Dto;

namespace RR_Nueva_Naturaleza.Service
{
    public interface IActuadorCultivoService
    {
        Task<IEnumerable<ActuadorCultivo>> GetActuadores(Guid? cultivoId);
        Task<ActuadorCultivo?> GetActuador(Guid id);
        Task<ServiceResponse> AddActuador(ActuadorCultivoCreateDto actuador, Guid usuarioId);
        Task<ServiceResponse> UpdateActuador(Guid id, ActuadorCultivoCreateDto actuador, Guid usuarioId);
        Task<ServiceResponse> DeleteActuador(Guid id, string motivo, Guid usuarioId);
        Task<ServiceResponse> Accionar(Guid id, AccionarActuadorDto accion, Guid usuarioId);
        Task<ServiceResponse> CambiarModo(Guid id, CambiarModoActuadorDto cambio, Guid usuarioId);
        Task<ServiceResponse> CambiarEstado(Guid id, CambiarEstadoDto cambio, Guid usuarioId);
    }
}
