using RR_Nueva_Naturaleza.Models;

namespace RR_Nueva_Naturaleza.Service
{
    public interface IUmbralMeteoService
    {
        Task<IEnumerable<UmbralMeteo>> GetUmbrales();
        Task<UmbralMeteo?> GetUmbral(Guid id);
    }
}
