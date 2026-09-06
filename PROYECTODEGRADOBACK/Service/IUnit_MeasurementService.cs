using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Models.Dto;

namespace RR_Nueva_Naturaleza.Service
{
    public interface IUnit_MeasurementService
    {
        Task<ServiceResponse> AddUnitMeasurement(CreateUnitDto unit);
        Task<Unit_Measurement?> GetUnitMeasurement(Guid UnitMeasurementId);
        Task<IEnumerable<Unit_MeasurementDto>> GetUnitMeasurements(); // El metodo returna una coleecion de objetos (Lista)
        Task<ServiceResponse> UpdateUnitMeasurement(Guid UnitMeasurementId, string name);
        Task<ServiceResponse> DeleteUnitMeasurement(Guid UnitMeasurementId);
    }
}
