using Microsoft.EntityFrameworkCore;
using RR_Nueva_Naturaleza.DAL;
using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Models.Dto;

namespace RR_Nueva_Naturaleza.Service
{
    public class UmbralCultivoService : IUmbralCultivoService
    {
        private static readonly string[] NivelesValidos = { "Baja", "Media", "Alta" };

        private readonly RR_Nueva_NaturalezaContext _context;
        private readonly IBitacoraCultivoService _bitacora;

        public UmbralCultivoService(RR_Nueva_NaturalezaContext context, IBitacoraCultivoService bitacora)
        {
            _context = context;
            _bitacora = bitacora;
        }

        public async Task<IEnumerable<UmbralCultivo>> GetUmbrales(Guid? cultivoId)
        {
            var query = ConsultaCompleta().Where(u => !u.Eliminado);
            if (cultivoId.HasValue) query = query.Where(u => u.CultivoId == cultivoId.Value);

            return await query.OrderBy(u => u.Cultivo!.Numero).ThenBy(u => u.Sensor!.Nombre).ToListAsync();
        }

        public async Task<UmbralCultivo?> GetUmbral(Guid id)
        {
            return await ConsultaCompleta().FirstOrDefaultAsync(u => u.Id == id && !u.Eliminado);
        }

        public async Task<ServiceResponse> AddUmbral(UmbralCultivoCreateDto umbral, Guid usuarioId)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                var error = await Validar(umbral);
                if (error != null) return Fallo(response, error);

                var nuevo = new UmbralCultivo
                {
                    Id = Guid.NewGuid(),
                    Activo = true,
                    CreadoPor = usuarioId,
                    FechaCreacion = DateTime.Now
                };
                Copiar(umbral, nuevo);

                _context.Umbrales_Cultivo.Add(nuevo);
                var nombres = await Nombres(nuevo);
                _bitacora.AgregarAuditoria(nuevo.CultivoId, "Umbral", nuevo.Id, nombres.Umbral, AccionesAuditoria.Creado,
                    Resumen(nuevo, nombres), umbral.Motivo, usuarioId);
                await _context.SaveChangesAsync();

                response.Result = ServiceResponseType.Succeded;
                response.Data = nuevo.Id;
            }
            catch (Exception ex)
            {
                response.Result = ServiceResponseType.Failed;
                response.ErrorMessage = ex.Message;
            }
            return response;
        }

        public async Task<ServiceResponse> UpdateUmbral(Guid id, UmbralCultivoCreateDto umbral, Guid usuarioId)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                var existente = await _context.Umbrales_Cultivo.FirstOrDefaultAsync(u => u.Id == id && !u.Eliminado);
                if (existente == null) return Fallo(response, "El umbral no existe");

                var error = await Validar(umbral);
                if (error != null) return Fallo(response, error);

                var antes = await Nombres(existente);
                var copiaAntes = new UmbralCultivo();
                Copiar(existente, copiaAntes);

                Copiar(umbral, existente);
                var despues = await Nombres(existente);

                var detalle = BitacoraCultivoService.DescribirCambios(
                    ("Sensor", antes.Sensor, despues.Sensor),
                    ("Etapa", antes.Etapa, despues.Etapa),
                    ("Valor mínimo", copiaAntes.ValorMin, existente.ValorMin),
                    ("Valor máximo", copiaAntes.ValorMax, existente.ValorMax),
                    ("Gravedad", copiaAntes.NivelGravedad, existente.NivelGravedad),
                    ("Actuador", antes.Actuador, despues.Actuador),
                    ("Acción al salir de rango", Accion(copiaAntes.EncenderActuador), Accion(existente.EncenderActuador)),
                    ("Revertir al normalizar", copiaAntes.RevertirAlNormalizar, existente.RevertirAlNormalizar),
                    ("Minutos para SMS", copiaAntes.MinutosEscalamientoSms, existente.MinutosEscalamientoSms),
                    ("Descripción", copiaAntes.Descripcion, existente.Descripcion));

                if (detalle != null)
                {
                    _bitacora.AgregarAuditoria(existente.CultivoId, "Umbral", existente.Id, despues.Umbral,
                        AccionesAuditoria.Modificado, detalle, umbral.Motivo, usuarioId);
                }
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

        // Un umbral inactivo no se evalúa: no dispara alarmas ni acciona actuadores
        public async Task<ServiceResponse> CambiarEstado(Guid id, CambiarEstadoDto cambio, Guid usuarioId)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                if (string.IsNullOrWhiteSpace(cambio.Motivo))
                    return Fallo(response, "El motivo es obligatorio para activar o desactivar un umbral");

                var existente = await _context.Umbrales_Cultivo.FirstOrDefaultAsync(u => u.Id == id && !u.Eliminado);
                if (existente == null) return Fallo(response, "El umbral no existe");

                if (existente.Activo != cambio.Activo)
                {
                    existente.Activo = cambio.Activo;
                    var nombres = await Nombres(existente);
                    _bitacora.AgregarAuditoria(existente.CultivoId, "Umbral", existente.Id, nombres.Umbral,
                        cambio.Activo ? AccionesAuditoria.Activado : AccionesAuditoria.Desactivado, null, cambio.Motivo, usuarioId);
                    await _context.SaveChangesAsync();
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

        // Eliminación lógica: las alarmas que ya generó se conservan
        public async Task<ServiceResponse> DeleteUmbral(Guid id, string motivo, Guid usuarioId)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                if (string.IsNullOrWhiteSpace(motivo)) return Fallo(response, "El motivo es obligatorio para eliminar un umbral");

                var existente = await _context.Umbrales_Cultivo.FirstOrDefaultAsync(u => u.Id == id && !u.Eliminado);
                if (existente == null) return Fallo(response, "El umbral no existe");

                existente.Eliminado = true;
                existente.Activo = false;
                var nombres = await Nombres(existente);
                _bitacora.AgregarAuditoria(existente.CultivoId, "Umbral", existente.Id, nombres.Umbral,
                    AccionesAuditoria.Eliminado, Resumen(existente, nombres), motivo, usuarioId);
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

        private IQueryable<UmbralCultivo> ConsultaCompleta()
        {
            return _context.Umbrales_Cultivo
                .Include(u => u.Cultivo)
                .Include(u => u.Sensor)
                .Include(u => u.Etapa)
                .Include(u => u.Actuador);
        }

        private static void Copiar(UmbralCultivoCreateDto origen, UmbralCultivo destino)
        {
            destino.CultivoId = origen.CultivoId;
            destino.SensorCultivoId = origen.SensorCultivoId;
            destino.EtapaCultivoId = origen.EtapaCultivoId;
            destino.ValorMin = origen.ValorMin;
            destino.ValorMax = origen.ValorMax;
            destino.NivelGravedad = origen.NivelGravedad;
            destino.ActuadorCultivoId = origen.ActuadorCultivoId;
            destino.EncenderActuador = origen.ActuadorCultivoId.HasValue ? origen.EncenderActuador : null;
            destino.RevertirAlNormalizar = origen.RevertirAlNormalizar;
            destino.MinutosEscalamientoSms = origen.MinutosEscalamientoSms;
            destino.Descripcion = origen.Descripcion;
        }

        private static void Copiar(UmbralCultivo origen, UmbralCultivo destino)
        {
            destino.ValorMin = origen.ValorMin;
            destino.ValorMax = origen.ValorMax;
            destino.NivelGravedad = origen.NivelGravedad;
            destino.EncenderActuador = origen.EncenderActuador;
            destino.RevertirAlNormalizar = origen.RevertirAlNormalizar;
            destino.MinutosEscalamientoSms = origen.MinutosEscalamientoSms;
            destino.Descripcion = origen.Descripcion;
        }

        // Nombres legibles para la auditoría (no IDs)
        private async Task<(string Umbral, string Sensor, string Etapa, string? Actuador)> Nombres(UmbralCultivo u)
        {
            var sensor = await _context.Sensores_Cultivo.Where(s => s.Id == u.SensorCultivoId).Select(s => s.Nombre).FirstOrDefaultAsync() ?? "?";
            var etapa = u.EtapaCultivoId.HasValue
                ? await _context.Etapas_Cultivo.Where(e => e.Id == u.EtapaCultivoId).Select(e => e.Nombre).FirstOrDefaultAsync() ?? "?"
                : "Todas";
            var actuador = u.ActuadorCultivoId.HasValue
                ? await _context.Actuadores_Cultivo.Where(a => a.Id == u.ActuadorCultivoId).Select(a => a.Nombre).FirstOrDefaultAsync()
                : null;

            return ($"{sensor} · etapa {etapa}", sensor, etapa, actuador);
        }

        private static string Resumen(UmbralCultivo u, (string Umbral, string Sensor, string Etapa, string? Actuador) n)
        {
            var rango = $"{u.ValorMin?.ToString("0.####") ?? "sin mín."} a {u.ValorMax?.ToString("0.####") ?? "sin máx."}";
            var accion = n.Actuador != null ? $"; {Accion(u.EncenderActuador)} {n.Actuador}" : string.Empty;
            var sms = u.MinutosEscalamientoSms.HasValue ? $"; SMS a los {u.MinutosEscalamientoSms} min" : string.Empty;
            return $"Rango {rango}; gravedad {u.NivelGravedad}{accion}{sms}";
        }

        private static string? Accion(bool? encender) => encender == null ? null : encender.Value ? "Encender" : "Apagar";

        // Sensor, etapa y actuador deben pertenecer al mismo cultivo del umbral y no estar eliminados
        private async Task<string?> Validar(UmbralCultivoCreateDto umbral)
        {
            if (!umbral.ValorMin.HasValue && !umbral.ValorMax.HasValue)
                return "Defina al menos un valor mínimo o máximo";
            if (umbral.ValorMin.HasValue && umbral.ValorMax.HasValue && umbral.ValorMin > umbral.ValorMax)
                return "El valor mínimo no puede ser mayor al máximo";
            if (!NivelesValidos.Contains(umbral.NivelGravedad))
                return "El nivel de gravedad debe ser Baja, Media o Alta";
            if (umbral.MinutosEscalamientoSms.HasValue && umbral.MinutosEscalamientoSms < 1)
                return "Los minutos para escalar a SMS deben ser al menos 1";

            if (!await _context.Sensores_Cultivo.AnyAsync(s => s.Id == umbral.SensorCultivoId && s.CultivoId == umbral.CultivoId && !s.Eliminado))
                return "El sensor no pertenece al cultivo seleccionado";

            if (umbral.EtapaCultivoId.HasValue &&
                !await _context.Etapas_Cultivo.AnyAsync(e => e.Id == umbral.EtapaCultivoId && e.CultivoId == umbral.CultivoId && !e.Eliminado))
                return "La etapa no pertenece al cultivo seleccionado";

            if (umbral.ActuadorCultivoId.HasValue)
            {
                if (!umbral.EncenderActuador.HasValue)
                    return "Indique si el actuador se enciende o se apaga al salir de rango";
                if (!await _context.Actuadores_Cultivo.AnyAsync(a => a.Id == umbral.ActuadorCultivoId && a.CultivoId == umbral.CultivoId && !a.Eliminado))
                    return "El actuador no pertenece al cultivo seleccionado";
            }

            return null;
        }

        private static ServiceResponse Fallo(ServiceResponse response, string mensaje)
        {
            response.Result = ServiceResponseType.Failed;
            response.ErrorMessage = mensaje;
            return response;
        }
    }
}
