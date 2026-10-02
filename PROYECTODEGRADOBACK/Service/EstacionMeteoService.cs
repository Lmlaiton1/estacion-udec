using Microsoft.EntityFrameworkCore;
using RR_Nueva_Naturaleza.DAL;
using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Models.Dto;

namespace RR_Nueva_Naturaleza.Service
{
    public class EstacionMeteoService : IEstacionMeteoService
    {
        private readonly RR_Nueva_NaturalezaContext _context;
        private readonly IAuditoriaMeteoService _auditoriaMeteoService;

        public EstacionMeteoService(RR_Nueva_NaturalezaContext context, IAuditoriaMeteoService auditoriaMeteoService)
        {
            _context = context;
            _auditoriaMeteoService = auditoriaMeteoService;
        }

        public async Task<IEnumerable<EstacionMeteo>> GetEstaciones()
        {
            return await _context.Estaciones_Meteo.ToListAsync();
        }

        public async Task<EstacionMeteo?> GetEstacion(Guid id)
        {
            return await _context.Estaciones_Meteo.FindAsync(id);
        }

        public async Task<ServiceResponse> AddEstacion(EstacionMeteoCreateDto estacion)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                var existeNumero = await _context.Estaciones_Meteo.AnyAsync(e => e.Numero == estacion.Numero);
                if (existeNumero)
                {
                    response.Result = ServiceResponseType.Failed;
                    response.ErrorMessage = $"Ya existe una estación con el número {estacion.Numero}";
                    return response;
                }

                var nueva = new EstacionMeteo
                {
                    Id = Guid.NewGuid(),
                    Numero = estacion.Numero,
                    Nombre = estacion.Nombre,
                    Ubicacion = estacion.Ubicacion,
                    Descripcion = estacion.Descripcion,
                    Activa = estacion.Activa,
                    FechaInstalacion = estacion.FechaInstalacion
                };

                _context.Estaciones_Meteo.Add(nueva);
                await _context.SaveChangesAsync();

                response.Result = ServiceResponseType.Succeded;
                response.Data = nueva;
            }
            catch (Exception ex)
            {
                response.Result = ServiceResponseType.Failed;
                response.ErrorMessage = ex.Message;
            }
            return response;
        }

        public async Task<ServiceResponse> UpdateEstacion(Guid id, EstacionMeteoUpdateDto estacion)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                var existente = await _context.Estaciones_Meteo.FindAsync(id);
                if (existente == null)
                {
                    response.Result = ServiceResponseType.Failed;
                    response.ErrorMessage = "La estación no existe";
                    return response;
                }

                existente.Nombre = estacion.Nombre;
                existente.Ubicacion = estacion.Ubicacion;
                existente.Descripcion = estacion.Descripcion;
                existente.Activa = estacion.Activa;
                existente.FechaInstalacion = estacion.FechaInstalacion;

                _context.Estaciones_Meteo.Update(existente);
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

        public async Task<ServiceResponse> DeleteEstacion(Guid id)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                var existente = await _context.Estaciones_Meteo.FindAsync(id);
                if (existente == null)
                {
                    response.Result = ServiceResponseType.Failed;
                    response.ErrorMessage = "La estación no existe";
                    return response;
                }

                _context.Estaciones_Meteo.Remove(existente);
                await _context.SaveChangesAsync();

                response.Result = ServiceResponseType.Succeded;
            }
            catch (DbUpdateException)
            {
                response.Result = ServiceResponseType.Failed;
                response.ErrorMessage = "No se puede eliminar: la estación tiene lecturas asociadas";
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
                    response.ErrorMessage = "El motivo es obligatorio para activar o desactivar una estación";
                    return response;
                }

                var existente = await _context.Estaciones_Meteo.FindAsync(id);
                if (existente == null)
                {
                    response.Result = ServiceResponseType.Failed;
                    response.ErrorMessage = "La estación no existe";
                    return response;
                }

                if (existente.Activa != cambio.Activo)
                {
                    existente.Activa = cambio.Activo;
                    _context.Estaciones_Meteo.Update(existente);
                    await _context.SaveChangesAsync();

                    await _auditoriaMeteoService.Registrar("Estacion", existente.Id, existente.Nombre, cambio.Activo, cambio.Motivo, usuarioId);
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
