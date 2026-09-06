using Microsoft.EntityFrameworkCore;
using RR_Nueva_Naturaleza.DAL;
using RR_Nueva_Naturaleza.Models;

namespace RR_Nueva_Naturaleza.Service
{
    public class Device_TypeService : IDevice_TypeService
    {

        private readonly RR_Nueva_NaturalezaContext _context;

        public Device_TypeService(RR_Nueva_NaturalezaContext context) //Constructor con inyeccion de dependencias
        {
            _context = context;
        }

        public async Task<ServiceResponse> AddDeviceType(string name)
        {
            try
            {
                await _context.Device_Types.AddAsync(new Device_Type()
                {
                    Id = Guid.NewGuid(),
                    Device_Type_Name = name
                });
                await _context.SaveChangesAsync();

                return new ServiceResponse()
                {
                    Result = ServiceResponseType.Succeded,
                    InformationMessage = "DeviceType add Correct"
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

        public async Task<Device_Type?> GetDeviceType(Guid DeviceTypeId)
        {
            return await _context.Device_Types.FindAsync(DeviceTypeId);
        }

        public async Task<IEnumerable<Device_Type>> GetDeviceTypes()
        {
            return await _context.Device_Types.ToListAsync();
        }

        public async Task<ServiceResponse> UpdateDeviceType(Guid DeviceTypeId, string name)
        {
            try
            {
                var deviceType = await _context.Device_Types.FindAsync(DeviceTypeId);
                if (deviceType == null)
                {
                    return new ServiceResponse()
                    {
                        Result = ServiceResponseType.Failed,
                        ErrorMessage = "Device Type don't exist"
                    };
                }
                deviceType.Device_Type_Name = name;
                _context.Device_Types.Update(deviceType);

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

        public async Task<ServiceResponse> DeleteDeviceType(Guid DeviceTypeId)
        {
            try
            {
                var deviceType = await _context.Device_Types.FindAsync(DeviceTypeId);

                if (deviceType == null)
                {
                    return new ServiceResponse()
                    {
                        Result = ServiceResponseType.Failed,
                        ErrorMessage = "Device Type don't exist"
                    };
                }
                _context.Device_Types.Remove(deviceType);
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
