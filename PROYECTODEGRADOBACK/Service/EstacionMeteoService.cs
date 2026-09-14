using Microsoft.EntityFrameworkCore;
using RR_Nueva_Naturaleza.DAL;
using RR_Nueva_Naturaleza.Models;

namespace RR_Nueva_Naturaleza.Service
{
    public class EstacionMeteoService : IEstacionMeteoService
    {
        private readonly RR_Nueva_NaturalezaContext _context;

        public EstacionMeteoService(RR_Nueva_NaturalezaContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<EstacionMeteo>> GetEstaciones()
        {
            return await _context.Estaciones_Meteo.ToListAsync();
        }

        public async Task<EstacionMeteo?> GetEstacion(Guid id)
        {
            return await _context.Estaciones_Meteo.FindAsync(id);
        }
    }
}
