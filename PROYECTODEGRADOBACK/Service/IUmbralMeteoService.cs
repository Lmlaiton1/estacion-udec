using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Models.Dto;

namespace RR_Nueva_Naturaleza.Service
{
    public interface IUmbralMeteoService
    {
        Task<IEnumerable<UmbralMeteo>> GetUmbrales();
        Task<UmbralMeteo?> GetUmbral(Guid id);
        Task<ServiceResponse> AddUmbral(UmbralMeteoCreateDto umbral, Guid creadoPor);
        Task<ServiceResponse> UpdateUmbral(Guid id, UmbralMeteoUpdateDto umbral, Guid usuarioId);
        Task<ServiceResponse> DeleteUmbral(Guid id, string motivo, Guid usuarioId);
    }
}
