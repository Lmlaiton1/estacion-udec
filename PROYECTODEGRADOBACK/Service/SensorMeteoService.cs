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
            var sensores = await _context.Sensores_Meteo.ToListAsync();
            var inactivos = await _context.Sensores_Meteo_Inactivos.ToListAsync();
            foreach (var sensor in sensores)
            {
                sensor.EstacionesInactivas = inactivos.Where(i => i.SensorId == sensor.Id)
                    .Select(i => i.EstacionNumero).OrderBy(n => n).ToList();
            }
            return sensores;
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

        // Sin EstacionNumero cambia el estado en todas las estaciones; con EstacionNumero solo en esa.
        public async Task<ServiceResponse> CambiarEstado(Guid id, CambiarEstadoSensorMeteoDto cambio, Guid usuarioId)
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

                var inactivos = await _context.Sensores_Meteo_Inactivos.Where(i => i.SensorId == id).ToListAsync();
                string detalle;

                if (cambio.EstacionNumero == null)
                {
                    // Activar en todas también limpia las desactivaciones puntuales
                    if (existente.Activo == cambio.Activo && (!cambio.Activo || inactivos.Count == 0))
                    {
                        response.Result = ServiceResponseType.Succeded;
                        return response;
                    }

                    existente.Activo = cambio.Activo;
                    if (cambio.Activo) _context.Sensores_Meteo_Inactivos.RemoveRange(inactivos);
                    detalle = "Todas las estaciones";
                }
                else
                {
                    var numero = cambio.EstacionNumero.Value;
                    var estaciones = await _context.Estaciones_Meteo.Select(e => e.Numero).ToListAsync();
                    if (!estaciones.Contains(numero))
                    {
                        response.Result = ServiceResponseType.Failed;
                        response.ErrorMessage = $"La estación {numero} no existe";
                        return response;
                    }

                    var marca = inactivos.FirstOrDefault(i => i.EstacionNumero == numero);
                    var activoEnEstacion = existente.Activo && marca == null;
                    if (activoEnEstacion == cambio.Activo)
                    {
                        response.Result = ServiceResponseType.Succeded;
                        return response;
                    }

                    if (!cambio.Activo)
                    {
                        _context.Sensores_Meteo_Inactivos.Add(new SensorMeteoInactivo { SensorId = id, EstacionNumero = numero });
                    }
                    else
                    {
                        if (marca != null) _context.Sensores_Meteo_Inactivos.Remove(marca);

                        // Estaba apagado en todas: se enciende el global y las demás estaciones quedan apagadas
                        if (!existente.Activo)
                        {
                            existente.Activo = true;
                            foreach (var otra in estaciones.Where(n => n != numero && !inactivos.Any(i => i.EstacionNumero == n)))
                            {
                                _context.Sensores_Meteo_Inactivos.Add(new SensorMeteoInactivo { SensorId = id, EstacionNumero = otra });
                            }
                        }
                    }
                    detalle = $"Estación {numero}";
                }

                _auditoriaMeteoService.Agregar("Sensor", existente.Id, existente.Nombre,
                    cambio.Activo ? "Activada" : "Desactivada", detalle, cambio.Motivo, usuarioId);
                await _context.SaveChangesAsync();

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
