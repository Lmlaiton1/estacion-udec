using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Models.Dto;

namespace RR_Nueva_Naturaleza.Service
{
    public interface IEstacionMeteoService
    {
        Task<IEnumerable<EstacionMeteo>> GetEstaciones();
        Task<EstacionMeteo?> GetEstacion(Guid id);
        Task<ServiceResponse> AddEstacion(EstacionMeteoCreateDto estacion);
        Task<ServiceResponse> UpdateEstacion(Guid id, EstacionMeteoUpdateDto estacion);
        Task<ServiceResponse> DeleteEstacion(Guid id);
        Task<ServiceResponse> CambiarEstado(Guid id, CambiarEstadoDto cambio, Guid usuarioId);
        Task<ServiceResponse> Reiniciar(Guid id, string motivo, Guid usuarioId);
        Task<ServiceResponse> ReiniciarTodas(string motivo, Guid usuarioId);
    }
}
