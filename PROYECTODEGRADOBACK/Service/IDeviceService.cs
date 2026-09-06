using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Models.Dto;
using RR_Nueva_Naturaleza.Models.Request;
using System.Threading.Tasks;

namespace RR_Nueva_Naturaleza.Service
{
    public interface IDeviceService
    {
        Task<ServiceResponse> AddActuator(ActuatorCreatePostDto actuator);
        Task<ServiceResponse> AddSensor(SensorCreatePostDto sensor);
        Task<ServiceResponse> AddDevice(string deviceName, string mark, string sn, string description, Guid systemTypeId, Guid deviceTypeId, Guid deviceStatusId);
        Task<Device?> GetDevice(Guid DeviceId);
        Task<IEnumerable<Device>> GetDevices(); // El metodo returna una coleecion de objetos (Lista)
        Task<IEnumerable<DeviceRequest>> GetActuators();
        Task<IEnumerable<DeviceDto>> GetAllActuators();
        Task<IEnumerable<SensorDto>> GetAllSensors();
        Task<List<ActuatorMode>> GetAllAsync();
        Task<ActuatorMode> UpdateModeAsync(Guid deviceId, bool isAutoMode);
        Task<IEnumerable<DeviceRequestSensors>> GetSensors();
        Task<List<SensorMeasurementDto>> GetLastMeasurements();
        Task<ServiceResponse> UpdateActuator(Guid DeviceId, ActuatorPostDto actuator);
        Task<ServiceResponse> UpdateSensor(Guid DeviceId, SensorPostDto sensor);
        Task<ServiceResponse> UpdateDevice(Guid DeviceId, string deviceName, string mark, string sn, string description, Guid systemTypeId, Guid deviceStatusId);
        Task<ServiceResponse> UpdateStateDevice(Guid DeviceId, string deviceStatus);
        Task<ServiceResponse> DeleteDevice(Guid DeviceId);
        Task<ServiceResponse> turnOnOffActuator(string nameDevice, string state);
        Task<List<ControlRuleResponseDto>> GetByDeviceAsync(Guid deviceId);
        Task<ControlRuleResponseDto?> UpdateAsync(Guid id, ControlRuleCreateDto dto);

    }
}
