using Microsoft.EntityFrameworkCore;
using RR_Nueva_Naturaleza.DAL;
using RR_Nueva_Naturaleza.Models;

namespace RR_Nueva_Naturaleza.Service
{
    public class SensorMeteoService : ISensorMeteoService
    {
        private readonly RR_Nueva_NaturalezaContext _context;

        public SensorMeteoService(RR_Nueva_NaturalezaContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<SensorMeteo>> GetSensores()
        {
            return await _context.Sensores_Meteo.ToListAsync();
        }

        public async Task<SensorMeteo?> GetSensor(Guid id)
        {
            return await _context.Sensores_Meteo.FindAsync(id);
        }
    }
}
