using Microsoft.EntityFrameworkCore;
using RR_Nueva_Naturaleza.DAL;
using RR_Nueva_Naturaleza.Models;

namespace RR_Nueva_Naturaleza.Service
{
    public class AlertaMeteoService : IAlertaMeteoService
    {
        private readonly RR_Nueva_NaturalezaContext _context;

        public AlertaMeteoService(RR_Nueva_NaturalezaContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<AlertaMeteo>> GetAlertas()
        {
            return await _context.Alertas_Meteo.ToListAsync();
        }

        public async Task<AlertaMeteo?> GetAlerta(Guid id)
        {
            return await _context.Alertas_Meteo.FindAsync(id);
        }
    }
}
