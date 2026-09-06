using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Models.Dto;

namespace RR_Nueva_Naturaleza.Service
{
    public interface IChecklistService
    {
        Task<ServiceResponse> AddChecklist(ChecklistHeaderDto checklistHeaderDto);
        Task<ServiceResponse> GetChecklist(Guid checklistId);
        Task<IEnumerable<ChecklistHeader>> GetAllChecklists();
        Task<IEnumerable<ChecklistHeader>> GetChecklistsByDate(DateTime startDate, DateTime endDate);
        Task<IEnumerable<ChecklistHeader>> GetChecklistsByUser(Guid userId);
        Task<IEnumerable<object>> GetSensorMeasurements(Guid deviceId, DateTime startDate, DateTime endDate);
        Task GenerateChecklistCsv(Stream output, DateTime startDate, DateTime endDate, Guid? userId);
        Task GeneratePdf(Stream output, Guid measurementTypeId, DateTime startDate, DateTime endDate, string chartImageBase64);
    }
}