using Microsoft.EntityFrameworkCore;
using RR_Nueva_Naturaleza.DAL;
using RR_Nueva_Naturaleza.Models;

namespace RR_Nueva_Naturaleza.Service
{
    public class ImpactService : IImpactService
    {

        private readonly RR_Nueva_NaturalezaContext _context;

        public ImpactService(RR_Nueva_NaturalezaContext context) //Constructor con inyeccion de dependencias
        {
            _context = context;
        }

        public async Task<ServiceResponse> AddImpact(string name)
        {
            try
            {
                await _context.Impacts.AddAsync(new Impact()
                {
                    Id = Guid.NewGuid(),
                    Impact_Type = name
                });
                await _context.SaveChangesAsync();

                return new ServiceResponse()
                {
                    Result = ServiceResponseType.Succeded,
                    InformationMessage = "Impact add Correct"
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

        public async Task<Impact?> GetImpact(Guid ImpactId)
        {
            return await _context.Impacts.FindAsync(ImpactId);
        }

        public async Task<IEnumerable<Impact>> GetImpacts()
        {
            return await _context.Impacts.ToListAsync();
        }

        public async Task<ServiceResponse> UpdateImpact(Guid ImpactId, string name)
        {
            try
            {
                var impact = await _context.Impacts.FindAsync(ImpactId);
                if (impact == null)
                {
                    return new ServiceResponse()
                    {
                        Result = ServiceResponseType.Failed,
                        ErrorMessage = "Impact don't exist"
                    };
                }
                impact.Impact_Type = name;
                _context.Impacts.Update(impact);

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

        public async Task<ServiceResponse> DeleteImpact(Guid ImpactId)
        {
            try
            {
                var impact = await _context.Impacts.FindAsync(ImpactId);

                if (impact == null)
                {
                    return new ServiceResponse()
                    {
                        Result = ServiceResponseType.Failed,
                        ErrorMessage = "Impact don't exist"
                    };
                }
                _context.Impacts.Remove(impact);
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
