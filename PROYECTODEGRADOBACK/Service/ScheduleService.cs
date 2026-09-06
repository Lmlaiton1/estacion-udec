using Microsoft.EntityFrameworkCore;
using RR_Nueva_Naturaleza.DAL;
using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Models.Dto;
using System.IO.Ports;

namespace RR_Nueva_Naturaleza.Service
{
    public class ScheduleService : IScheduleService
    {
        private readonly RR_Nueva_NaturalezaContext _context;

        public ScheduleService(RR_Nueva_NaturalezaContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ScheduleDto>> GetAllAsync()
        {
            return await _context.Schedules.Include(s => s.Device)
                .Select(s => new ScheduleDto
                {
                    Id = s.Id,
                    Hour = s.Hour,
                    Minute = s.Minute,
                    DurationSeconds = s.DurationSeconds,
                    DeviceId = s.DeviceId,
                    DeviceName = s.Device!.Device_Name
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<ScheduleDto>> GetByDeviceIdAsync(Guid deviceId)
        {
            return await _context.Schedules
                .Include(s => s.Device)
                .Where(s => s.DeviceId == deviceId)
                .Select(s => new ScheduleDto
                {
                    Id = s.Id,
                    Hour = s.Hour,
                    Minute = s.Minute,
                    DurationSeconds = s.DurationSeconds,
                    DeviceId = s.DeviceId,
                    DeviceName = s.Device!.Device_Name
                })
                .ToListAsync();
        }

        public async Task<ScheduleDto> GetByIdAsync(Guid id)
        {
            var s = await _context.Schedules.Include(x => x.Device).FirstOrDefaultAsync(x => x.Id == id);
            if (s == null) return null!;

            return new ScheduleDto
            {
                Id = s.Id,
                Hour = s.Hour,
                Minute = s.Minute,
                DurationSeconds = s.DurationSeconds,
                DeviceId = s.DeviceId,
                DeviceName = s.Device!.Device_Name
            };
        }

        public async Task<ScheduleDto> CreateAsync(ScheduleDto dto)
        {
            var schedule = new Schedule
            {
                Id = Guid.NewGuid(),
                Hour = dto.Hour,
                Minute = dto.Minute,
                DurationSeconds = dto.DurationSeconds,
                DeviceId = dto.DeviceId
            };

            _context.Schedules.Add(schedule);
            await _context.SaveChangesAsync();

            return await GetByIdAsync(schedule.Id);
        }

        public async Task<ScheduleDto> UpdateAsync(Guid id, ScheduleDto dto)
        {
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule == null) return null!;

            schedule.Hour = dto.Hour;
            schedule.Minute = dto.Minute;
            schedule.DurationSeconds = dto.DurationSeconds;
            schedule.DeviceId = dto.DeviceId;

            await _context.SaveChangesAsync();
            return await GetByIdAsync(schedule.Id);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule == null) return false;

            _context.Schedules.Remove(schedule);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<SerialPortConfigDto> GetConfig()
        {
            var s = await _context.SerialPortConfigs.FirstOrDefaultAsync();
            if (s == null) return null!;

            return new SerialPortConfigDto
            {
                PortName = s.PortName,
                BaudRate = s.BaudRate,
            };
        }

        public async Task<SerialPortConfigDto> UpdateConfig(SerialPortConfigDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.PortName))
                return null;

            var available = SerialPort.GetPortNames();
            if (!available.Contains(dto.PortName))
                return null;

            var cfg = await _context.SerialPortConfigs.FirstOrDefaultAsync();
            if (cfg == null)
            {
                cfg = new SerialPortConfig { PortName = dto.PortName, BaudRate = dto.BaudRate };
                _context.SerialPortConfigs.Add(cfg);
            }
            else
            {
                cfg.PortName = dto.PortName;
                cfg.BaudRate = dto.BaudRate;
                _context.SerialPortConfigs.Update(cfg);
            }

            await _context.SaveChangesAsync();
            return new SerialPortConfigDto
            {
                PortName = cfg.PortName,
                BaudRate = cfg.BaudRate,
            };
        }
    }
}
