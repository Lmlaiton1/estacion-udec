using RR_Nueva_Naturaleza.Models;

namespace RR_Nueva_Naturaleza.Service
{
    public interface IMeasurementService
    {
        Task<ServiceResponse> AddMeasurement(decimal value, DateTime date, Guid deviceId, Guid unitMeasurementId);
        Task<Measurement?> GetMeasurement(Guid MeasurementId);
        Task<IEnumerable<Measurement>> GetMeasurements();
        Task<ServiceResponse> UpdateMeasurement(Guid MeasurementId, decimal value, DateTime date);
        Task<ServiceResponse> DeleteMeasurement(Guid MeasurementId);
        Task<IEnumerable<Measurement>> GetMeasurementsByDate(Guid measurementTypeId, DateTime startDate, DateTime endDate);
        Task GetMeasurementsForPdf(Stream output, Guid measurementTypeId, DateTime startDate, DateTime endDate, string chartImageBase64);
        Task GenerateAllMeasurementsCsv(Stream output, DateTime startDate, DateTime endDate, Guid? userId);

    }
}
