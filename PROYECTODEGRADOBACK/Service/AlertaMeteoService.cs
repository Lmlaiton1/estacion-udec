using Microsoft.EntityFrameworkCore;
using RR_Nueva_Naturaleza.DAL;
using RR_Nueva_Naturaleza.Models;

namespace RR_Nueva_Naturaleza.Service
{
    public class AlertaMeteoService : IAlertaMeteoService
    {
        private readonly RR_Nueva_NaturalezaContext _context;
        private readonly IAuditoriaMeteoService _auditoriaMeteoService;

        public AlertaMeteoService(RR_Nueva_NaturalezaContext context, IAuditoriaMeteoService auditoriaMeteoService)
        {
            _context = context;
            _auditoriaMeteoService = auditoriaMeteoService;
        }

        public async Task<IEnumerable<AlertaMeteo>> GetAlertas()
        {
            return await _context.Alertas_Meteo.Include(a => a.VistoPor).ToListAsync();
        }

        public async Task<AlertaMeteo?> GetAlerta(Guid id)
        {
            return await _context.Alertas_Meteo.Include(a => a.VistoPor).FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<ServiceResponse> MarcarVista(Guid id, string accionTomada, Guid usuarioId)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                if (string.IsNullOrWhiteSpace(accionTomada))
                {
                    response.Result = ServiceResponseType.Failed;
                    response.ErrorMessage = "Debe indicar qué se hizo con la alerta (queda en la auditoría)";
                    return response;
                }

                var alerta = await _context.Alertas_Meteo.FindAsync(id);
                if (alerta == null)
                {
                    response.Result = ServiceResponseType.Failed;
                    response.ErrorMessage = "La alerta no existe";
                    return response;
                }
                if (alerta.Visto)
                {
                    response.Result = ServiceResponseType.Failed;
                    response.ErrorMessage = "La alerta ya fue marcada como vista";
                    return response;
                }

                alerta.Visto = true;
                alerta.VistoPorId = usuarioId;
                alerta.FechaVisto = DateTime.Now;
                alerta.AccionTomada = accionTomada.Trim();
                _context.Alertas_Meteo.Update(alerta);

                _auditoriaMeteoService.Agregar("Alerta", alerta.Id, $"{alerta.Variable} · Estación {alerta.EstacionNumero}", "Vista",
                    alerta.Mensaje, alerta.AccionTomada, usuarioId);
                await _context.SaveChangesAsync();

                response.Result = ServiceResponseType.Succeded;
            }
            catch (Exception ex)
            {
                response.Result = ServiceResponseType.Failed;
                response.ErrorMessage = ex.Message;
            }
            return response;
        }
    }
}
