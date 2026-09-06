using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Models.Dto;

namespace RR_Nueva_Naturaleza.Service
{
    public interface IDevice_StatusService
    {
        Task<ServiceResponse> AddDeviceStatus(string name);
        Task<Device_Status?> GetDeviceStatus(Guid DeviceStatusId);
        Task<IEnumerable<Device_StatusDto>> GetDeviceStates(); // El metodo returna una coleecion de objetos (Lista)
        Task<ServiceResponse> UpdateDeviceStatus(Guid DeviceStatusId, string name);
        Task<ServiceResponse> DeleteDeviceStatus(Guid DeviceStatusId);
    }
}
