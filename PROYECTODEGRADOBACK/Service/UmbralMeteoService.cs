using Microsoft.EntityFrameworkCore;
using RR_Nueva_Naturaleza.DAL;
using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Models.Dto;

namespace RR_Nueva_Naturaleza.Service
{
    public class UmbralMeteoService : IUmbralMeteoService
    {
        private readonly RR_Nueva_NaturalezaContext _context;

        public UmbralMeteoService(RR_Nueva_NaturalezaContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<UmbralMeteo>> GetUmbrales()
        {
            return await _context.Umbrales_Meteo.ToListAsync();
        }

        public async Task<UmbralMeteo?> GetUmbral(Guid id)
        {
            return await _context.Umbrales_Meteo.FindAsync(id);
        }

        public async Task<ServiceResponse> AddUmbral(UmbralMeteoCreateDto umbral, Guid creadoPor)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                var nuevo = new UmbralMeteo
                {
                    Id = Guid.NewGuid(),
                    EstacionNumero = umbral.EstacionNumero,
                    Variable = umbral.Variable,
                    ValorMin = umbral.ValorMin,
                    ValorMax = umbral.ValorMax,
                    NivelGravedad = umbral.NivelGravedad,
                    Activo = umbral.Activo,
                    Descripcion = umbral.Descripcion,
                    CreadoPor = creadoPor,
                    FechaCreacion = DateTime.Now
                };

                _context.Umbrales_Meteo.Add(nuevo);
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

        public async Task<ServiceResponse> UpdateUmbral(Guid id, UmbralMeteoUpdateDto umbral)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                var existente = await _context.Umbrales_Meteo.FindAsync(id);
                if (existente == null)
                {
                    response.Result = ServiceResponseType.Failed;
                    response.ErrorMessage = "El umbral no existe";
                    return response;
                }

                existente.EstacionNumero = umbral.EstacionNumero;
                existente.Variable = umbral.Variable;
                existente.ValorMin = umbral.ValorMin;
                existente.ValorMax = umbral.ValorMax;
                existente.NivelGravedad = umbral.NivelGravedad;
                existente.Activo = umbral.Activo;
                existente.Descripcion = umbral.Descripcion;

                _context.Umbrales_Meteo.Update(existente);
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

        public async Task<ServiceResponse> DeleteUmbral(Guid id)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                var existente = await _context.Umbrales_Meteo.FindAsync(id);
                if (existente == null)
                {
                    response.Result = ServiceResponseType.Failed;
                    response.ErrorMessage = "El umbral no existe";
                    return response;
                }

                _context.Umbrales_Meteo.Remove(existente);
                await _context.SaveChangesAsync();

                response.Result = ServiceResponseType.Succeded;
            }
            catch (DbUpdateException)
            {
                response.Result = ServiceResponseType.Failed;
                response.ErrorMessage = "No se puede eliminar: el umbral tiene alertas asociadas";
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
