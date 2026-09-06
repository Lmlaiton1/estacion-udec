using RR_Nueva_Naturaleza.Models;

namespace RR_Nueva_Naturaleza.Service
{
    public interface IImpactService
    {
        Task<ServiceResponse> AddImpact(string name);
        Task<Impact?> GetImpact(Guid ImpactId);
        Task<IEnumerable<Impact>> GetImpacts(); // El metodo returna una coleecion de objetos (Lista)
        Task<ServiceResponse> UpdateImpact(Guid ImpactId, string name);
        Task<ServiceResponse> DeleteImpact(Guid ImpactId);
    }
}
