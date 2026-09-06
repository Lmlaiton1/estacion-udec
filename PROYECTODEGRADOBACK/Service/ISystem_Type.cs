using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Models.Dto;

namespace RR_Nueva_Naturaleza.Service
{
    public interface ISystem_Type
    {
        Task<ServiceResponse> AddSystemType(string name);
        Task<System_Type?> GetSystemType(Guid System_TypeId);
        Task<IEnumerable<System_TypeDto>> GetSystemTypes(); // El metodo returna una coleecion de objetos (Lista)
        Task<ServiceResponse> UpdateSystemType(Guid System_TypeId, string name);
        Task<ServiceResponse> DeleteSystemType(Guid System_TypeId);
    }
}
