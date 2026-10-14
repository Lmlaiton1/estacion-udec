using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Models.Dto;

namespace RR_Nueva_Naturaleza.Service
{
    public interface ICultivoService
    {
        Task<IEnumerable<Cultivo>> GetCultivos();
        Task<Cultivo?> GetCultivo(Guid id);
        Task<ServiceResponse> AddCultivo(CultivoCreateDto cultivo, Guid usuarioId);
        Task<ServiceResponse> UpdateCultivo(Guid id, CultivoCreateDto cultivo, Guid usuarioId);
        Task<ServiceResponse> CambiarEstado(Guid id, CambiarEstadoDto cambio, Guid usuarioId);
        Task<ServiceResponse> DeleteCultivo(Guid id, string motivo, Guid usuarioId);

        Task<IEnumerable<EtapaCultivo>> GetEtapas(Guid cultivoId);
        Task<ServiceResponse> AddEtapa(EtapaCultivoCreateDto etapa, Guid usuarioId);
        Task<ServiceResponse> UpdateEtapa(Guid id, EtapaCultivoCreateDto etapa, Guid usuarioId);
        Task<ServiceResponse> DeleteEtapa(Guid id, string motivo, Guid usuarioId);
        Task<ServiceResponse> ActivarEtapa(Guid id, string motivo, Guid usuarioId);
        Task<ServiceResponse> QuitarEtapaActual(Guid id, string motivo, Guid usuarioId);
    }
}
