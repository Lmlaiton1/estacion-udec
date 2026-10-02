using Microsoft.EntityFrameworkCore;
using RR_Nueva_Naturaleza.DAL;
using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Models.Dto;

namespace RR_Nueva_Naturaleza.Service
{
    public class SensorMeteoService : ISensorMeteoService
    {
        private readonly RR_Nueva_NaturalezaContext _context;
        private readonly IAuditoriaMeteoService _auditoriaMeteoService;

        public SensorMeteoService(RR_Nueva_NaturalezaContext context, IAuditoriaMeteoService auditoriaMeteoService)
        {
            _context = context;
            _auditoriaMeteoService = auditoriaMeteoService;
        }

        public async Task<IEnumerable<SensorMeteo>> GetSensores()
        {
            return await _context.Sensores_Meteo.ToListAsync();
        }

        public async Task<SensorMeteo?> GetSensor(Guid id)
        {
            return await _context.Sensores_Meteo.FindAsync(id);
        }

        public async Task<ServiceResponse> AddSensor(SensorMeteoCreateDto sensor)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                var existente = await _context.Sensores_Meteo.FindAsync(sensor.Id);
                if (existente != null)
                {
                    response.Result = ServiceResponseType.Failed;
                    response.ErrorMessage = $"Ya existe un sensor con el id {sensor.Id}";
                    return response;
                }

                var nuevo = new SensorMeteo
                {
                    Id = sensor.Id,
                    Nombre = sensor.Nombre,
                    Tipo = sensor.Tipo,
                    Variables = sensor.Variables,
                    Descripcion = sensor.Descripcion
                };

                _context.Sensores_Meteo.Add(nuevo);
                await _context.SaveChangesAsync();

                response.Result = ServiceResponseType.Succeded;
                response.Data = nuevo;
            }
            catch (Exception ex)
            {
                response.Result = ServiceResponseType.Failed;
                response.ErrorMessage = ex.Message;
            }
            return response;
        }

        public async Task<ServiceResponse> UpdateSensor(Guid id, SensorMeteoUpdateDto sensor)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                var existente = await _context.Sensores_Meteo.FindAsync(id);
                if (existente == null)
                {
                    response.Result = ServiceResponseType.Failed;
                    response.ErrorMessage = "El sensor no existe";
                    return response;
                }

                existente.Nombre = sensor.Nombre;
                existente.Tipo = sensor.Tipo;
                existente.Variables = sensor.Variables;
                existente.Descripcion = sensor.Descripcion;

                _context.Sensores_Meteo.Update(existente);
                await _context.SaveChangesAsync();

                response.Result = ServiceResponseType.Succeded;
                response.Data = existente;
            }
            catch (Exception ex)
            {
                response.Result = ServiceResponseType.Failed;
                response.ErrorMessage = ex.Message;
            }
            return response;
        }

        public async Task<ServiceResponse> DeleteSensor(Guid id)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                var existente = await _context.Sensores_Meteo.FindAsync(id);
                if (existente == null)
                {
                    response.Result = ServiceResponseType.Failed;
                    response.ErrorMessage = "El sensor no existe";
                    return response;
                }

                _context.Sensores_Meteo.Remove(existente);
                await _context.SaveChangesAsync();

                response.Result = ServiceResponseType.Succeded;
            }
            catch (DbUpdateException)
            {
                response.Result = ServiceResponseType.Failed;
                response.ErrorMessage = "No se puede eliminar: el sensor tiene lecturas asociadas";
            }
            catch (Exception ex)
            {
                response.Result = ServiceResponseType.Failed;
                response.ErrorMessage = ex.Message;
            }
            return response;
        }

        public async Task<ServiceResponse> CambiarEstado(Guid id, CambiarEstadoDto cambio, Guid usuarioId)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                if (string.IsNullOrWhiteSpace(cambio.Motivo))
                {
                    response.Result = ServiceResponseType.Failed;
                    response.ErrorMessage = "El motivo es obligatorio para activar o desactivar un sensor";
                    return response;
                }

                var existente = await _context.Sensores_Meteo.FindAsync(id);
                if (existente == null)
                {
                    response.Result = ServiceResponseType.Failed;
                    response.ErrorMessage = "El sensor no existe";
                    return response;
                }

                if (existente.Activo != cambio.Activo)
                {
                    existente.Activo = cambio.Activo;
                    _context.Sensores_Meteo.Update(existente);
                    await _context.SaveChangesAsync();

                    await _auditoriaMeteoService.Registrar("Sensor", existente.Id, existente.Nombre, cambio.Activo, cambio.Motivo, usuarioId);
                }

                response.Result = ServiceResponseType.Succeded;
            }
            catch (Exception ex)
            {
                response.Result = ServiceResponseType.Failed;
                response.ErrorMessage = ex.Message;
            }
            return response;
        }
    }
}
