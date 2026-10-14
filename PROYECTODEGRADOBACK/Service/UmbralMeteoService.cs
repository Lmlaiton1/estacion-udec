using Microsoft.EntityFrameworkCore;
using RR_Nueva_Naturaleza.DAL;
using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Models.Dto;

namespace RR_Nueva_Naturaleza.Service
{
    public class UmbralMeteoService : IUmbralMeteoService
    {
        private readonly RR_Nueva_NaturalezaContext _context;
        private readonly IAuditoriaMeteoService _auditoriaMeteoService;

        public UmbralMeteoService(RR_Nueva_NaturalezaContext context, IAuditoriaMeteoService auditoriaMeteoService)
        {
            _context = context;
            _auditoriaMeteoService = auditoriaMeteoService;
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
                if (string.IsNullOrWhiteSpace(umbral.Motivo))
                    return Fallo(response, "Debe indicar el motivo (queda en la auditoría)");

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
                _auditoriaMeteoService.Agregar("Umbral", nuevo.Id, Nombre(nuevo), "Creado", Resumen(nuevo), umbral.Motivo.Trim(), creadoPor);
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

        public async Task<ServiceResponse> UpdateUmbral(Guid id, UmbralMeteoUpdateDto umbral, Guid usuarioId)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                if (string.IsNullOrWhiteSpace(umbral.Motivo))
                    return Fallo(response, "Debe indicar el motivo del cambio (queda en la auditoría)");

                var existente = await _context.Umbrales_Meteo.FindAsync(id);
                if (existente == null)
                    return Fallo(response, "El umbral no existe");

                var detalle = DetalleCambios(existente, umbral);
                var cambioEstado = existente.Activo != umbral.Activo;

                existente.EstacionNumero = umbral.EstacionNumero;
                existente.Variable = umbral.Variable;
                existente.ValorMin = umbral.ValorMin;
                existente.ValorMax = umbral.ValorMax;
                existente.NivelGravedad = umbral.NivelGravedad;
                existente.Activo = umbral.Activo;
                existente.Descripcion = umbral.Descripcion;

                // Si solo cambió el estado se registra como Activado/Desactivado; si hubo más cambios, como Modificado
                if (detalle != null)
                {
                    var accion = cambioEstado && detalle.Count == 1
                        ? (umbral.Activo ? "Activado" : "Desactivado")
                        : "Modificado";
                    _auditoriaMeteoService.Agregar("Umbral", existente.Id, Nombre(existente), accion,
                        string.Join("; ", detalle), umbral.Motivo.Trim(), usuarioId);
                }

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

        public async Task<ServiceResponse> DeleteUmbral(Guid id, string motivo, Guid usuarioId)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                if (string.IsNullOrWhiteSpace(motivo))
                    return Fallo(response, "Debe indicar el motivo de la eliminación (queda en la auditoría)");

                var existente = await _context.Umbrales_Meteo.FindAsync(id);
                if (existente == null)
                    return Fallo(response, "El umbral no existe");

                _context.Umbrales_Meteo.Remove(existente);
                _auditoriaMeteoService.Agregar("Umbral", existente.Id, Nombre(existente), "Eliminado", Resumen(existente), motivo.Trim(), usuarioId);
                await _context.SaveChangesAsync();

                response.Result = ServiceResponseType.Succeded;
            }
            catch (DbUpdateException)
            {
                response.Result = ServiceResponseType.Failed;
                response.ErrorMessage = "No se puede eliminar: el umbral tiene alertas asociadas. Desactívelo en su lugar";
            }
            catch (Exception ex)
            {
                response.Result = ServiceResponseType.Failed;
                response.ErrorMessage = ex.Message;
            }
            return response;
        }

        // Nombre legible para la auditoría, ej.: "temperatura · Estación 2"
        private static string Nombre(UmbralMeteo u) => $"{u.Variable} · {Estacion(u.EstacionNumero)}";

        private static string Estacion(int? numero) => numero.HasValue ? $"Estación {numero}" : "Todas las estaciones";

        private static string Valor(decimal? valor) => valor?.ToString("0.####") ?? "—";

        private static string Resumen(UmbralMeteo u) =>
            $"Rango {Valor(u.ValorMin)} a {Valor(u.ValorMax)}; gravedad {u.NivelGravedad}; {(u.Activo ? "activo" : "inactivo")}";

        // Lista de cambios "Campo: antes → después". Null si no cambió nada.
        private static List<string>? DetalleCambios(UmbralMeteo antes, UmbralMeteoUpdateDto despues)
        {
            var cambios = new List<string>();
            if (antes.EstacionNumero != despues.EstacionNumero)
                cambios.Add($"Estación: {Estacion(antes.EstacionNumero)} → {Estacion(despues.EstacionNumero)}");
            if (antes.Variable != despues.Variable)
                cambios.Add($"Variable: {antes.Variable} → {despues.Variable}");
            if (antes.ValorMin != despues.ValorMin)
                cambios.Add($"Mín: {Valor(antes.ValorMin)} → {Valor(despues.ValorMin)}");
            if (antes.ValorMax != despues.ValorMax)
                cambios.Add($"Máx: {Valor(antes.ValorMax)} → {Valor(despues.ValorMax)}");
            if (antes.NivelGravedad != despues.NivelGravedad)
                cambios.Add($"Gravedad: {antes.NivelGravedad} → {despues.NivelGravedad}");
            if ((antes.Descripcion ?? "") != (despues.Descripcion ?? ""))
                cambios.Add($"Descripción: {(string.IsNullOrEmpty(antes.Descripcion) ? "—" : antes.Descripcion)} → {(string.IsNullOrEmpty(despues.Descripcion) ? "—" : despues.Descripcion)}");
            if (antes.Activo != despues.Activo)
                cambios.Add($"Estado: {(antes.Activo ? "Activo" : "Inactivo")} → {(despues.Activo ? "Activo" : "Inactivo")}");

            return cambios.Count > 0 ? cambios : null;
        }

        private static ServiceResponse Fallo(ServiceResponse response, string mensaje)
        {
            response.Result = ServiceResponseType.Failed;
            response.ErrorMessage = mensaje;
            return response;
        }
    }
}
