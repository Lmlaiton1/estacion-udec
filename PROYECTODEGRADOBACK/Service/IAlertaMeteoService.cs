using RR_Nueva_Naturaleza.Models;

namespace RR_Nueva_Naturaleza.Service
{
    public interface IAlertaMeteoService
    {
        Task<IEnumerable<AlertaMeteo>> GetAlertas();
        Task<AlertaMeteo?> GetAlerta(Guid id);
        Task<ServiceResponse> MarcarVista(Guid id, string accionTomada, Guid usuarioId);
    }
}
