using RR_Nueva_Naturaleza.Models;

namespace RR_Nueva_Naturaleza.Service
{
    public interface IAuditService
    {
        Task<ServiceResponse> AddAudit(string action, string observation, Guid userId, Guid deviceId);
        Task<Audit?> GetAudit(Guid AuditId);
        Task<IEnumerable<Audit>> GetAudits(); // El metodo returna una coleecion de objetos (Lista)
        Task<ServiceResponse> UpdateAudit(Guid AuditId, string action, string observation, Guid deviceId);
        Task<ServiceResponse> DeleteAudit(Guid AuditId);
        Task<IEnumerable<Audit>> GetAuditByUserDate(Guid userId, DateTime startDate, DateTime endDate);
        Task GetAuditByUserDateForPdf(Stream output, Guid userId, DateTime startDate, DateTime endDate, string chartImageBase64);
    }
}
