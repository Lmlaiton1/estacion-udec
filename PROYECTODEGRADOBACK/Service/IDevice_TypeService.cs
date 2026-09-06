using RR_Nueva_Naturaleza.Models;

namespace RR_Nueva_Naturaleza.Service
{
    public interface IDevice_TypeService
    {
        Task<ServiceResponse> AddDeviceType(string name);
        Task<Device_Type?> GetDeviceType(Guid DeviceTypeId);
        Task<IEnumerable<Device_Type>> GetDeviceTypes(); // El metodo returna una coleecion de objetos (Lista)
        Task<ServiceResponse> UpdateDeviceType(Guid DeviceTypeId, string name);
        Task<ServiceResponse> DeleteDeviceType(Guid DeviceTypeId);
    }
}
