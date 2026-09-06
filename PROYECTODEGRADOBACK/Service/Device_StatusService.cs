using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RR_Nueva_Naturaleza.DAL;
using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Models.Dto;
using RR_Nueva_Naturaleza.Models.Request;

namespace RR_Nueva_Naturaleza.Service
{
    public class Device_StatusService : IDevice_StatusService
    {
        private readonly RR_Nueva_NaturalezaContext _context;
        private readonly IMapper _mapper;

        public Device_StatusService(RR_Nueva_NaturalezaContext context, IMapper mapper ) //Constructor con inyeccion de dependencias
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<ServiceResponse> AddDeviceStatus(string name)
        {
            try
            {
                await _context.Device_States.AddAsync(new Device_Status()
                {
                    Id = Guid.NewGuid(),
                    State_Name = name
                });
                await _context.SaveChangesAsync();

                return new ServiceResponse()
                {
                    Result = ServiceResponseType.Succeded,
                    InformationMessage = "Device Status add Correct"
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse()
                {
                    Result = ServiceResponseType.Failed,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<Device_Status?> GetDeviceStatus(Guid DeviceStatusId)
        {
            return await _context.Device_States.FindAsync(DeviceStatusId);
        }

        public async Task<IEnumerable<Device_StatusDto>> GetDeviceStates()
        {
            var device = await _context.Device_States.ToListAsync();
            return _mapper.Map<IEnumerable<Device_StatusDto>>(device); ;
        }

        public async Task<ServiceResponse> UpdateDeviceStatus(Guid DeviceStatusId, string name)
        {
            try
            {
                var deviceStatus = await _context.Device_States.FindAsync(DeviceStatusId);
                if (deviceStatus == null)
                {
                    return new ServiceResponse()
                    {
                        Result = ServiceResponseType.Failed,
                        ErrorMessage = "Device Status don't exist"
                    };
                }
                deviceStatus.State_Name = name;
                _context.Device_States.Update(deviceStatus);

                await _context.SaveChangesAsync();
                return new ServiceResponse()
                {
                    Result = ServiceResponseType.Succeded
                };

            }
            catch (Exception ex)
            {
                return new ServiceResponse()
                {
                    Result = ServiceResponseType.Failed,
                    ErrorMessage = ex.Message

                };
            }
        }

        public async Task<ServiceResponse> DeleteDeviceStatus(Guid DeviceStatusId)
        {
            try
            {
                var deviceStatus = await _context.Device_States.FindAsync(DeviceStatusId);

                if (deviceStatus == null)
                {
                    return new ServiceResponse()
                    {
                        Result = ServiceResponseType.Failed,
                        ErrorMessage = "Device Status don't exist"
                    };
                }
                _context.Device_States.Remove(deviceStatus);
                await _context.SaveChangesAsync();

                return new ServiceResponse()
                {
                    Result = ServiceResponseType.Succeded

                };

            }
            catch (Exception ex)
            {
                return new ServiceResponse()
                {
                    Result = ServiceResponseType.Failed,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}
