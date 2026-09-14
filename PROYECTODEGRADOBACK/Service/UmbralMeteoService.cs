using Microsoft.EntityFrameworkCore;
using RR_Nueva_Naturaleza.DAL;
using RR_Nueva_Naturaleza.Models;

namespace RR_Nueva_Naturaleza.Service
{
    public class UmbralMeteoService : IUmbralMeteoService
    {
        private readonly RR_Nueva_NaturalezaContext _context;

        public UmbralMeteoService(RR_Nueva_NaturalezaContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<UmbralMeteo>> GetUmbrales()
        {
            return await _context.Umbrales_Meteo.ToListAsync();
        }

        public async Task<UmbralMeteo?> GetUmbral(Guid id)
        {
            return await _context.Umbrales_Meteo.FindAsync(id);
        }
    }
}
