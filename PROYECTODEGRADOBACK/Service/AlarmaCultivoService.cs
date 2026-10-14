using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RR_Nueva_Naturaleza.DAL;
using RR_Nueva_Naturaleza.Hubs;
using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Models.Dto;

namespace RR_Nueva_Naturaleza.Service
{
    // Flujo de atención: Activa -> Vista -> EnAtencion -> Resuelta.
    // Cada paso queda en la bitácora con usuario y fecha; los campos VistaPor/AtendidaPor/ResueltaPor
    // guardan el primero que hizo cada paso para mostrarlo rápido en la tabla.
    public class AlarmaCultivoService : IAlarmaCultivoService
    {
        private readonly RR_Nueva_NaturalezaContext _context;
        private readonly IBitacoraCultivoService _bitacora;
        private readonly IHubContext<CultivoHub> _hub;

        public AlarmaCultivoService(RR_Nueva_NaturalezaContext context, IBitacoraCultivoService bitacora, IHubContext<CultivoHub> hub)
        {
            _context = context;
            _bitacora = bitacora;
            _hub = hub;
        }

        public async Task<IEnumerable<AlarmaCultivo>> GetAlarmas(Guid? cultivoId, bool soloAbiertas, DateTime? desde, DateTime? hasta)
        {
            var query = ConsultaCompleta().Where(a => !a.Eliminado);

            if (cultivoId.HasValue) query = query.Where(a => a.CultivoId == cultivoId.Value);
            if (soloAbiertas) query = query.Where(a => a.Estado != EstadosAlarma.Resuelta && a.Estado != EstadosAlarma.Anulada);
            if (desde.HasValue) query = query.Where(a => a.FechaDisparo >= desde.Value);
            if (hasta.HasValue) query = query.Where(a => a.FechaDisparo <= hasta.Value);

            return await query.OrderByDescending(a => a.FechaDisparo).Take(500).ToListAsync();
        }

        public async Task<AlarmaCultivo?> GetAlarma(Guid id)
        {
            return await ConsultaCompleta().FirstOrDefaultAsync(a => a.Id == id && !a.Eliminado);
        }

        public async Task<ServiceResponse> MarcarVista(Guid id, Guid usuarioId)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                var alarma = await _context.Alarmas_Cultivo.FirstOrDefaultAsync(a => a.Id == id && !a.Eliminado);
                if (alarma == null) return Fallo(response, "La alarma no existe");
                if (alarma.Estado == EstadosAlarma.Resuelta) return Fallo(response, "La alarma ya está resuelta");
                if (alarma.Estado == EstadosAlarma.Anulada) return Fallo(response, "La alarma está anulada; reactívela primero");

                // Cada persona que la ve queda registrada, pero solo una vez
                var yaLaVio = await _context.Bitacora_Alarmas_Cultivo
                    .AnyAsync(b => b.AlarmaCultivoId == id && b.Accion == "Vista" && b.UsuarioId == usuarioId);

                if (!yaLaVio)
                {
                    if (alarma.VistaPorId == null)
                    {
                        alarma.VistaPorId = usuarioId;
                        alarma.FechaVista = DateTime.Now;
                    }
                    if (alarma.Estado == EstadosAlarma.Activa) alarma.Estado = EstadosAlarma.Vista;

                    _bitacora.AgregarBitacoraAlarma(id, "Vista", null, usuarioId);
                    await _context.SaveChangesAsync();
                    await NotificarCambio(alarma);
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

        // "Cómo tomó el control": el comentario es obligatorio.
        public async Task<ServiceResponse> TomarControl(Guid id, string comentario, Guid usuarioId)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                if (string.IsNullOrWhiteSpace(comentario)) return Fallo(response, "Describa cómo va a tomar el control de la alarma");

                var alarma = await _context.Alarmas_Cultivo.FirstOrDefaultAsync(a => a.Id == id && !a.Eliminado);
                if (alarma == null) return Fallo(response, "La alarma no existe");
                if (alarma.Estado == EstadosAlarma.Resuelta) return Fallo(response, "La alarma ya está resuelta");
                if (alarma.Estado == EstadosAlarma.Anulada) return Fallo(response, "La alarma está anulada; reactívela primero");

                var ahora = DateTime.Now;

                // Tomar el control implica haberla visto
                if (alarma.VistaPorId == null)
                {
                    alarma.VistaPorId = usuarioId;
                    alarma.FechaVista = ahora;
                    _bitacora.AgregarBitacoraAlarma(id, "Vista", null, usuarioId);
                }

                // Si otra persona toma el control, queda la última como responsable; la bitácora conserva a ambas
                alarma.AtendidaPorId = usuarioId;
                alarma.FechaAtencion = ahora;
                alarma.Estado = EstadosAlarma.EnAtencion;

                _bitacora.AgregarBitacoraAlarma(id, "TomoControl", comentario.Trim(), usuarioId);
                await _context.SaveChangesAsync();
                await NotificarCambio(alarma);

                response.Result = ServiceResponseType.Succeded;
            }
            catch (Exception ex)
            {
                response.Result = ServiceResponseType.Failed;
                response.ErrorMessage = ex.Message;
            }
            return response;
        }

        public async Task<ServiceResponse> Comentar(Guid id, string comentario, Guid usuarioId)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                if (string.IsNullOrWhiteSpace(comentario)) return Fallo(response, "El comentario no puede estar vacío");

                var alarma = await _context.Alarmas_Cultivo.FirstOrDefaultAsync(a => a.Id == id && !a.Eliminado);
                if (alarma == null) return Fallo(response, "La alarma no existe");

                _bitacora.AgregarBitacoraAlarma(id, "Comentario", comentario.Trim(), usuarioId);
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

        // "Quién la solucionó y cómo": el comentario es obligatorio.
        public async Task<ServiceResponse> Resolver(Guid id, string comentario, Guid usuarioId)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                if (string.IsNullOrWhiteSpace(comentario)) return Fallo(response, "Describa cómo se solucionó la alarma");

                var alarma = await _context.Alarmas_Cultivo.FirstOrDefaultAsync(a => a.Id == id && !a.Eliminado);
                if (alarma == null) return Fallo(response, "La alarma no existe");
                if (alarma.Estado == EstadosAlarma.Resuelta) return Fallo(response, "La alarma ya está resuelta");
                if (alarma.Estado == EstadosAlarma.Anulada) return Fallo(response, "La alarma está anulada; reactívela primero");

                var ahora = DateTime.Now;

                if (alarma.VistaPorId == null)
                {
                    alarma.VistaPorId = usuarioId;
                    alarma.FechaVista = ahora;
                    _bitacora.AgregarBitacoraAlarma(id, "Vista", null, usuarioId);
                }
                if (alarma.AtendidaPorId == null)
                {
                    alarma.AtendidaPorId = usuarioId;
                    alarma.FechaAtencion = ahora;
                }

                alarma.ResueltaPorId = usuarioId;
                alarma.FechaResolucion = ahora;
                alarma.Solucion = comentario.Trim();
                alarma.Estado = EstadosAlarma.Resuelta;

                _bitacora.AgregarBitacoraAlarma(id, "Resuelta", comentario.Trim(), usuarioId);
                await _context.SaveChangesAsync();
                await NotificarCambio(alarma);

                response.Result = ServiceResponseType.Succeded;
            }
            catch (Exception ex)
            {
                response.Result = ServiceResponseType.Failed;
                response.ErrorMessage = ex.Message;
            }
            return response;
        }

        // Corrección administrativa: reclasificar la gravedad o corregir el texto de la solución
        public async Task<ServiceResponse> UpdateAlarma(Guid id, AlarmaCultivoUpdateDto cambio, Guid usuarioId)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                if (string.IsNullOrWhiteSpace(cambio.Motivo)) return Fallo(response, "El motivo es obligatorio para modificar una alarma");
                if (!new[] { "Baja", "Media", "Alta" }.Contains(cambio.NivelGravedad))
                    return Fallo(response, "El nivel de gravedad debe ser Baja, Media o Alta");

                var alarma = await _context.Alarmas_Cultivo.FirstOrDefaultAsync(a => a.Id == id && !a.Eliminado);
                if (alarma == null) return Fallo(response, "La alarma no existe");

                var nuevaSolucion = alarma.Estado == EstadosAlarma.Resuelta && !string.IsNullOrWhiteSpace(cambio.Solucion)
                    ? cambio.Solucion.Trim()
                    : alarma.Solucion;

                var detalle = BitacoraCultivoService.DescribirCambios(
                    ("Gravedad", alarma.NivelGravedad, cambio.NivelGravedad),
                    ("Solución", alarma.Solucion, nuevaSolucion));
                if (detalle == null) return Fallo(response, "No hay cambios para guardar");

                alarma.NivelGravedad = cambio.NivelGravedad;
                alarma.Solucion = nuevaSolucion;

                _bitacora.AgregarBitacoraAlarma(id, "Modificada", $"{detalle}. Motivo: {cambio.Motivo.Trim()}", usuarioId);
                _bitacora.AgregarAuditoria(alarma.CultivoId, "Alarma", alarma.Id, NombreAlarma(alarma),
                    AccionesAuditoria.Modificado, detalle, cambio.Motivo, usuarioId);
                await _context.SaveChangesAsync();
                await NotificarCambio(alarma);

                response.Result = ServiceResponseType.Succeded;
            }
            catch (Exception ex)
            {
                response.Result = ServiceResponseType.Failed;
                response.ErrorMessage = ex.Message;
            }
            return response;
        }

        // Desactivar: la alarma se descarta (ej. falsa alarma por sensor desconectado). Si el valor sigue
        // fuera de rango, la próxima lectura dispara una alarma nueva.
        public async Task<ServiceResponse> Anular(Guid id, string motivo, Guid usuarioId)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                if (string.IsNullOrWhiteSpace(motivo)) return Fallo(response, "El motivo es obligatorio para anular una alarma");

                var alarma = await _context.Alarmas_Cultivo.FirstOrDefaultAsync(a => a.Id == id && !a.Eliminado);
                if (alarma == null) return Fallo(response, "La alarma no existe");
                if (alarma.Estado == EstadosAlarma.Anulada) return Fallo(response, "La alarma ya está anulada");

                var estadoAnterior = alarma.Estado;
                alarma.Estado = EstadosAlarma.Anulada;

                _bitacora.AgregarBitacoraAlarma(id, "Anulada", motivo.Trim(), usuarioId);
                _bitacora.AgregarAuditoria(alarma.CultivoId, "Alarma", alarma.Id, NombreAlarma(alarma),
                    AccionesAuditoria.Anulada, $"Estado: {estadoAnterior} → Anulada", motivo, usuarioId);
                await _context.SaveChangesAsync();
                await NotificarCambio(alarma);

                response.Result = ServiceResponseType.Succeded;
            }
            catch (Exception ex)
            {
                response.Result = ServiceResponseType.Failed;
                response.ErrorMessage = ex.Message;
            }
            return response;
        }

        // Volver a activar una alarma anulada o resuelta. Recupera el estado según lo que ya se había hecho.
        public async Task<ServiceResponse> Reactivar(Guid id, string motivo, Guid usuarioId)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                if (string.IsNullOrWhiteSpace(motivo)) return Fallo(response, "El motivo es obligatorio para reactivar una alarma");

                var alarma = await _context.Alarmas_Cultivo.FirstOrDefaultAsync(a => a.Id == id && !a.Eliminado);
                if (alarma == null) return Fallo(response, "La alarma no existe");
                if (alarma.Estado != EstadosAlarma.Anulada && alarma.Estado != EstadosAlarma.Resuelta)
                    return Fallo(response, "Solo se puede reactivar una alarma anulada o resuelta");

                // No puede haber dos alarmas abiertas del mismo umbral: el worker suma ocurrencias a una sola
                var otraAbierta = await _context.Alarmas_Cultivo.AnyAsync(a => a.UmbralCultivoId == alarma.UmbralCultivoId && a.Id != id &&
                    !a.Eliminado && a.Estado != EstadosAlarma.Resuelta && a.Estado != EstadosAlarma.Anulada);
                if (otraAbierta) return Fallo(response, "Ya hay otra alarma abierta del mismo umbral; atienda esa");

                var estadoAnterior = alarma.Estado;
                alarma.Estado = alarma.AtendidaPorId != null && estadoAnterior == EstadosAlarma.Anulada ? EstadosAlarma.EnAtencion
                    : alarma.VistaPorId != null ? EstadosAlarma.Vista
                    : EstadosAlarma.Activa;

                // Al reabrir una resuelta se limpia la resolución (queda en la bitácora)
                if (estadoAnterior == EstadosAlarma.Resuelta)
                {
                    alarma.ResueltaPorId = null;
                    alarma.FechaResolucion = null;
                    alarma.Solucion = null;
                }

                _bitacora.AgregarBitacoraAlarma(id, "Reactivada", motivo.Trim(), usuarioId);
                _bitacora.AgregarAuditoria(alarma.CultivoId, "Alarma", alarma.Id, NombreAlarma(alarma),
                    AccionesAuditoria.Reactivada, $"Estado: {estadoAnterior} → {alarma.Estado}", motivo, usuarioId);
                await _context.SaveChangesAsync();
                await NotificarCambio(alarma);

                response.Result = ServiceResponseType.Succeded;
            }
            catch (Exception ex)
            {
                response.Result = ServiceResponseType.Failed;
                response.ErrorMessage = ex.Message;
            }
            return response;
        }

        // Eliminación lógica: deja de verse en la lista, la auditoría la conserva
        public async Task<ServiceResponse> DeleteAlarma(Guid id, string motivo, Guid usuarioId)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                if (string.IsNullOrWhiteSpace(motivo)) return Fallo(response, "El motivo es obligatorio para eliminar una alarma");

                var alarma = await _context.Alarmas_Cultivo.FirstOrDefaultAsync(a => a.Id == id && !a.Eliminado);
                if (alarma == null) return Fallo(response, "La alarma no existe");

                var estadoAnterior = alarma.Estado;
                alarma.Eliminado = true;
                if (alarma.Estado != EstadosAlarma.Resuelta) alarma.Estado = EstadosAlarma.Anulada;

                _bitacora.AgregarBitacoraAlarma(id, "Eliminada", motivo.Trim(), usuarioId);
                _bitacora.AgregarAuditoria(alarma.CultivoId, "Alarma", alarma.Id, NombreAlarma(alarma),
                    AccionesAuditoria.Eliminado, $"Estado al eliminar: {estadoAnterior}", motivo, usuarioId);
                await _context.SaveChangesAsync();
                await NotificarCambio(alarma);

                response.Result = ServiceResponseType.Succeded;
            }
            catch (Exception ex)
            {
                response.Result = ServiceResponseType.Failed;
                response.ErrorMessage = ex.Message;
            }
            return response;
        }

        private static string NombreAlarma(AlarmaCultivo a) => $"{a.Variable} · {a.FechaDisparo:dd/MM/yyyy HH:mm}";

        private IQueryable<AlarmaCultivo> ConsultaCompleta()
        {
            return _context.Alarmas_Cultivo
                .Include(a => a.Cultivo)
                .Include(a => a.VistaPor)
                .Include(a => a.AtendidaPor)
                .Include(a => a.ResueltaPor)
                .Include(a => a.Bitacora!).ThenInclude(b => b.Usuario)
                .AsSplitQuery();
        }

        // Avisa a las demás pantallas abiertas para que actualicen el estado (sin mostrar notificación nueva)
        private async Task NotificarCambio(AlarmaCultivo alarma)
        {
            await _hub.Clients.All.SendAsync(CultivoHub.EventoAlarma, new
            {
                tipo = "Actualizada",
                id = alarma.Id,
                cultivoId = alarma.CultivoId,
                estado = alarma.Estado
            });
        }

        private static ServiceResponse Fallo(ServiceResponse response, string mensaje)
        {
            response.Result = ServiceResponseType.Failed;
            response.ErrorMessage = mensaje;
            return response;
        }
    }
}
