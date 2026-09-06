using RR_Nueva_Naturaleza.Models;

namespace RR_Nueva_Naturaleza.Service
{
    public interface IRolService
    {
        Task<ServiceResponse> AddRol(string name);
        Task<Rol?> GetRol(Guid RolId);
        Task<IEnumerable<Rol>> GetRols(); // El metodo returna una coleecion de objetos (Lista)
        Task<ServiceResponse> UpdateRol(Guid RolId, string name);
        Task<ServiceResponse> DeleteRol(Guid RolId);
    }
}
