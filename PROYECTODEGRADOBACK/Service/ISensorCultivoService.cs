using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Models.Dto;

namespace RR_Nueva_Naturaleza.Service
{
    public interface ISensorCultivoService
    {
        Task<IEnumerable<SensorCultivoDto>> GetSensores(Guid? cultivoId);
        Task<SensorCultivo?> GetSensor(Guid id);
        Task<ServiceResponse> AddSensor(SensorCultivoCreateDto sensor, Guid usuarioId);
        Task<ServiceResponse> UpdateSensor(Guid id, SensorCultivoCreateDto sensor, Guid usuarioId);
        Task<ServiceResponse> DeleteSensor(Guid id, string motivo, Guid usuarioId);
        Task<ServiceResponse> CambiarEstado(Guid id, CambiarEstadoDto cambio, Guid usuarioId);
        Task<IEnumerable<LecturaCultivo>> GetLecturas(Guid sensorId, int horas);
    }
}
