using Microsoft.EntityFrameworkCore;
using RR_Nueva_Naturaleza.DAL;
using RR_Nueva_Naturaleza.Models;

namespace RR_Nueva_Naturaleza.Service
{
    public class RolService : IRolService
    {
        private readonly RR_Nueva_NaturalezaContext _context;

        public RolService(RR_Nueva_NaturalezaContext context) //Constructor con inyeccion de dependencias
        {
            _context = context;
        }

        public async Task<ServiceResponse> AddRol(string name)
        {
            try
            {
                await _context.Rols.AddAsync(new Rol()
                {
                    Id = Guid.NewGuid(),
                    Name = name
                });
                await _context.SaveChangesAsync();

                return new ServiceResponse()
                {
                    Result = ServiceResponseType.Succeded,
                    InformationMessage = "Rol add Correct"
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

        public async Task<Rol?> GetRol(Guid RolId)
        {
            return await _context.Rols.FindAsync(RolId);
        }

        public async Task<IEnumerable<Rol>> GetRols()
        {
            return await _context.Rols.ToListAsync();
        }

        public async Task<ServiceResponse> UpdateRol(Guid RolId, string name)
        {
            try
            {
                var rol = await _context.Rols.FindAsync(RolId);
                if (rol == null)
                {
                    return new ServiceResponse()
                    {
                        Result = ServiceResponseType.Failed,
                        ErrorMessage = "Rol don't exist"
                    };
                }
                rol.Name = name;
                _context.Rols.Update(rol);

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

        public async Task<ServiceResponse> DeleteRol(Guid RolId)
        {
            try
            {
                var rol = await _context.Rols.FindAsync(RolId);

                if (rol == null)
                {
                    return new ServiceResponse()
                    {
                        Result = ServiceResponseType.Failed,
                        ErrorMessage = "Rol don't exist"
                    };
                }
                _context.Rols.Remove(rol);
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
