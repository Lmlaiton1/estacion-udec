using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Models.Dto;

namespace RR_Nueva_Naturaleza.Service
{
    public interface ISensorMeteoService
    {
        Task<IEnumerable<SensorMeteo>> GetSensores();
        Task<SensorMeteo?> GetSensor(Guid id);
        Task<ServiceResponse> AddSensor(SensorMeteoCreateDto sensor);
        Task<ServiceResponse> UpdateSensor(Guid id, SensorMeteoUpdateDto sensor);
        Task<ServiceResponse> DeleteSensor(Guid id);
        Task<ServiceResponse> CambiarEstado(Guid id, CambiarEstadoDto cambio, Guid usuarioId);
    }
}
