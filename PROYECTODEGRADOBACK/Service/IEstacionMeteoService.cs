using RR_Nueva_Naturaleza.Models;

namespace RR_Nueva_Naturaleza.Service
{
    public interface IEstacionMeteoService
    {
        Task<IEnumerable<EstacionMeteo>> GetEstaciones();
        Task<EstacionMeteo?> GetEstacion(Guid id);
    }
}
