using Microsoft.EntityFrameworkCore;
using RR_Nueva_Naturaleza.DAL;
using RR_Nueva_Naturaleza.Models;

namespace RR_Nueva_Naturaleza.Service
{
    public class LecturaMeteoService : ILecturaMeteoService
    {
        private readonly RR_Nueva_NaturalezaContext _context;

        public LecturaMeteoService(RR_Nueva_NaturalezaContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<LecturaMeteo>> GetLecturas()
        {
            return await _context.Lecturas_Meteo.ToListAsync();
        }

        public async Task<LecturaMeteo?> GetLectura(Guid id)
        {
            return await _context.Lecturas_Meteo.FindAsync(id);
        }

        public async Task<IEnumerable<LecturaMeteo>> GetLecturasByEstacion(int numero)
        {
            return await _context.Lecturas_Meteo
                .Where(l => l.EstacionNumero == numero)
                .OrderByDescending(l => l.FechaHora)
                .ToListAsync();
        }

        public async Task<IEnumerable<LecturaMeteo>> GetLecturasByEstacionAndVariable(int numero, string variable)
        {
            return await _context.Lecturas_Meteo
                .Where(l => l.EstacionNumero == numero && l.Variable == variable)
                .OrderByDescending(l => l.FechaHora)
                .ToListAsync();
        }
    }
}
