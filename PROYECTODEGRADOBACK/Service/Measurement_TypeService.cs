using Microsoft.EntityFrameworkCore;
using RR_Nueva_Naturaleza.DAL;
using RR_Nueva_Naturaleza.Models;

namespace RR_Nueva_Naturaleza.Service
{
    public class Measurement_TypeService : IMeasurement_TypeService
    {
        private readonly RR_Nueva_NaturalezaContext _context;

        public Measurement_TypeService(RR_Nueva_NaturalezaContext context) //Constructor con inyeccion de dependencias
        {
            _context = context;
        }

        public async Task<ServiceResponse> AddMeasurementType(string name)
        {
            try
            {
                await _context.Measurement_Types.AddAsync(new Measurement_Type()
                {
                    Id = Guid.NewGuid(),
                    Name = name
                });
                await _context.SaveChangesAsync();

                return new ServiceResponse()
                {
                    Result = ServiceResponseType.Succeded,
                    InformationMessage = "Measurement Type add Correct"
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

        public async Task<Measurement_Type?> GetMeasurementType(Guid MeasurementTypeId)
        {
            return await _context.Measurement_Types.FindAsync(MeasurementTypeId);
        }

        public async Task<IEnumerable<Measurement_Type>> GetMeasurementTypes()
        {
            return await _context.Measurement_Types.ToListAsync();
        }

        public async Task<ServiceResponse> UpdateMeasurementType(Guid MeasurementTypeId, string name)
        {
            try
            {
                var measurementType = await _context.Measurement_Types.FindAsync(MeasurementTypeId);
                if (measurementType == null)
                {
                    return new ServiceResponse()
                    {
                        Result = ServiceResponseType.Failed,
                        ErrorMessage = "Measurement Type don't exist"
                    };
                }
                measurementType.Name = name;
                _context.Measurement_Types.Update(measurementType);

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

        public async Task<ServiceResponse> DeleteMeasurementType(Guid MeasurementTypeId)
        {
            try
            {
                var measurementType = await _context.Measurement_Types.FindAsync(MeasurementTypeId);

                if (measurementType == null)
                {
                    return new ServiceResponse()
                    {
                        Result = ServiceResponseType.Failed,
                        ErrorMessage = "Measurement Type don't exist"
                    };
                }
                _context.Measurement_Types.Remove(measurementType);
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
