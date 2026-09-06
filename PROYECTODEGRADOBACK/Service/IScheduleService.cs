using RR_Nueva_Naturaleza.Models.Dto;

namespace RR_Nueva_Naturaleza.Service
{
    public interface IScheduleService
    {
        Task<IEnumerable<ScheduleDto>> GetAllAsync();
        Task<IEnumerable<ScheduleDto>> GetByDeviceIdAsync(Guid deviceId);
        Task<ScheduleDto> GetByIdAsync(Guid id);
        Task<ScheduleDto> CreateAsync(ScheduleDto schedule);
        Task<ScheduleDto> UpdateAsync(Guid id, ScheduleDto schedule);
        Task<SerialPortConfigDto> GetConfig();
        Task<SerialPortConfigDto> UpdateConfig(SerialPortConfigDto dto);
        Task<bool> DeleteAsync(Guid id);
    }
}
