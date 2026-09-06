using RR_Nueva_Naturaleza.Models;

namespace RR_Nueva_Naturaleza.Service
{
    public interface IMeasurement_TypeService
    {
        Task<ServiceResponse> AddMeasurementType(string name);
        Task<Measurement_Type?> GetMeasurementType(Guid MeasurementTypeId);
        Task<IEnumerable<Measurement_Type>> GetMeasurementTypes(); // El metodo returna una coleecion de objetos (Lista)
        Task<ServiceResponse> UpdateMeasurementType(Guid MeasurementTypeId, string name);
        Task<ServiceResponse> DeleteMeasurementType(Guid MeasurementTypeId);
    }
}
