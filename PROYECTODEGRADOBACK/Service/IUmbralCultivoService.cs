using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Models.Dto;

namespace RR_Nueva_Naturaleza.Service
{
    public interface IUmbralCultivoService
    {
        Task<IEnumerable<UmbralCultivo>> GetUmbrales(Guid? cultivoId);
        Task<UmbralCultivo?> GetUmbral(Guid id);
        Task<ServiceResponse> AddUmbral(UmbralCultivoCreateDto umbral, Guid usuarioId);
        Task<ServiceResponse> UpdateUmbral(Guid id, UmbralCultivoCreateDto umbral, Guid usuarioId);
        Task<ServiceResponse> CambiarEstado(Guid id, CambiarEstadoDto cambio, Guid usuarioId);
        Task<ServiceResponse> DeleteUmbral(Guid id, string motivo, Guid usuarioId);
    }
}
