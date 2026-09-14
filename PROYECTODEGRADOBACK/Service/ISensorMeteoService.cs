using RR_Nueva_Naturaleza.Models;

namespace RR_Nueva_Naturaleza.Service
{
    public interface ISensorMeteoService
    {
        Task<IEnumerable<SensorMeteo>> GetSensores();
        Task<SensorMeteo?> GetSensor(Guid id);
    }
}
