using RR_Nueva_Naturaleza.Models;

namespace RR_Nueva_Naturaleza.Service
{
    public interface IAlertaMeteoService
    {
        Task<IEnumerable<AlertaMeteo>> GetAlertas();
        Task<AlertaMeteo?> GetAlerta(Guid id);
    }
}
