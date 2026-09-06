using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RR_Nueva_Naturaleza.DAL;
using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Models.Dto;
using RR_Nueva_Naturaleza.Models.Request;

namespace RR_Nueva_Naturaleza.Service
{
    public class Unit_MeasurementService : IUnit_MeasurementService
    {
        private readonly RR_Nueva_NaturalezaContext _context;
        private readonly IMapper _mapper;

        public Unit_MeasurementService(RR_Nueva_NaturalezaContext context, IMapper mapper) //Constructor con inyeccion de dependencias
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<ServiceResponse> AddUnitMeasurement(CreateUnitDto unit)
        {
            try
            {
                await _context.Unit_Measurements.AddAsync(new Unit_Measurement()
                {
                    Id = Guid.NewGuid(),
                    Name = unit.Name
                });
                await _context.SaveChangesAsync();

                return new ServiceResponse()
                {
                    Result = ServiceResponseType.Succeded,
                    InformationMessage = "Unit Measurement add Correct"
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

        public async Task<Unit_Measurement?> GetUnitMeasurement(Guid UnitMeasurementId)
        {
            return await _context.Unit_Measurements.FindAsync(UnitMeasurementId);
        }

        public async Task<IEnumerable<Unit_MeasurementDto>> GetUnitMeasurements()
        {
            var units = await _context.Unit_Measurements.ToListAsync();
            return _mapper.Map<IEnumerable<Unit_MeasurementDto>>(units); ;
        }

        public async Task<ServiceResponse> UpdateUnitMeasurement(Guid UnitMeasurementId, string name)
        {
            try
            {
                var unit_Measurement = await _context.Unit_Measurements.FindAsync(UnitMeasurementId);
                if (unit_Measurement == null)
                {
                    return new ServiceResponse()
                    {
                        Result = ServiceResponseType.Failed,
                        ErrorMessage = "Unit Measurement don't exist"
                    };
                }
                unit_Measurement.Name = name;
                _context.Unit_Measurements.Update(unit_Measurement);

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

        public async Task<ServiceResponse> DeleteUnitMeasurement(Guid UnitMeasurementId)
        {
            try
            {
                var unit_Measurement = await _context.Unit_Measurements.FindAsync(UnitMeasurementId);

                if (unit_Measurement == null)
                {
                    return new ServiceResponse()
                    {
                        Result = ServiceResponseType.Failed,
                        ErrorMessage = "Unit Measurement don't exist"
                    };
                }
                _context.Unit_Measurements.Remove(unit_Measurement);
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
