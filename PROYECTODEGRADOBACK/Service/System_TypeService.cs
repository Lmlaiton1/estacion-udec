using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RR_Nueva_Naturaleza.DAL;
using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Models.Dto;
using RR_Nueva_Naturaleza.Models.Request;

namespace RR_Nueva_Naturaleza.Service
{
    public class System_TypeService : ISystem_Type
    {
        private readonly RR_Nueva_NaturalezaContext _context;
        private readonly IMapper _mapper;

        public System_TypeService(RR_Nueva_NaturalezaContext context, IMapper mapper) //Constructor con inyeccion de dependencias
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<ServiceResponse> AddSystemType(string name)
        {
            try
            {
                await _context.System_Types.AddAsync(new System_Type()
                {
                    Id = Guid.NewGuid(),
                    System_Type_Name = name
                });
                await _context.SaveChangesAsync();

                return new ServiceResponse()
                {
                    Result = ServiceResponseType.Succeded,
                    InformationMessage = "System_Type add Correct"
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

        public async Task<System_Type?> GetSystemType(Guid System_TypeId)
        {
            return await _context.System_Types.FindAsync(System_TypeId);
        }

        public async Task<IEnumerable<System_TypeDto>> GetSystemTypes()
        {
            var system = await _context.System_Types.ToListAsync();
            return _mapper.Map<IEnumerable<System_TypeDto>>(system);
        }

        public async Task<ServiceResponse> UpdateSystemType(Guid System_TypeId, string name)
        {
            try
            {
                var system_Type = await _context.System_Types.FindAsync(System_TypeId);
                if (system_Type == null)
                {
                    return new ServiceResponse()
                    {
                        Result = ServiceResponseType.Failed,
                        ErrorMessage = " don't exist"
                    };
                }
                system_Type.System_Type_Name = name;
                _context.System_Types.Update(system_Type);

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

        public async Task<ServiceResponse> DeleteSystemType(Guid System_TypeId)
        {
            try
            {
                var system_Type = await _context.System_Types.FindAsync(System_TypeId);

                if (system_Type == null)
                {
                    return new ServiceResponse()
                    {
                        Result = ServiceResponseType.Failed,
                        ErrorMessage = "System_Type don't exist"
                    };
                }
                _context.System_Types.Remove(system_Type);
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
