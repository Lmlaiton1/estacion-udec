using Microsoft.EntityFrameworkCore;
using RR_Nueva_Naturaleza.DAL;
using RR_Nueva_Naturaleza.Models;

namespace RR_Nueva_Naturaleza.Service
{
    public class BitacoraCultivoService : IBitacoraCultivoService
    {
        private readonly RR_Nueva_NaturalezaContext _context;

        public BitacoraCultivoService(RR_Nueva_NaturalezaContext context)
        {
            _context = context;
        }

        public void AgregarEvento(Guid cultivoId, string tipoDispositivo, Guid dispositivoId, string nombreDispositivo,
            string accion, string origen, string motivo, Guid? usuarioId, Guid? alarmaId = null, Guid? umbralId = null)
        {
            _context.Eventos_Dispositivo_Cultivo.Add(new EventoDispositivoCultivo
            {
                Id = Guid.NewGuid(),
                CultivoId = cultivoId,
                TipoDispositivo = tipoDispositivo,
                DispositivoId = dispositivoId,
                NombreDispositivo = nombreDispositivo,
                Accion = accion,
                Origen = origen,
                Motivo = motivo,
                UsuarioId = usuarioId,
                AlarmaCultivoId = alarmaId,
                UmbralCultivoId = umbralId,
                Fecha = DateTime.Now
            });
        }

        public void AgregarBitacoraAlarma(Guid alarmaId, string accion, string? comentario, Guid? usuarioId)
        {
            _context.Bitacora_Alarmas_Cultivo.Add(new BitacoraAlarmaCultivo
            {
                Id = Guid.NewGuid(),
                AlarmaCultivoId = alarmaId,
                Accion = accion,
                Comentario = comentario,
                UsuarioId = usuarioId,
                Fecha = DateTime.Now
            });
        }

        public void AgregarAuditoria(Guid? cultivoId, string tipoEntidad, Guid entidadId, string nombreEntidad,
            string accion, string? detalle, string? motivo, Guid usuarioId)
        {
            _context.Auditoria_Cultivo.Add(new AuditoriaCultivo
            {
                Id = Guid.NewGuid(),
                CultivoId = cultivoId,
                TipoEntidad = tipoEntidad,
                EntidadId = entidadId,
                NombreEntidad = Recortar(nombreEntidad, 150)!,
                Accion = accion,
                Detalle = Recortar(detalle, 2000),
                Motivo = Recortar(string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim(), 500),
                UsuarioId = usuarioId,
                Fecha = DateTime.Now
            });
        }

        // Arma el detalle "Campo: antes → después" solo con los campos que cambiaron.
        // Devuelve null si no cambió nada.
        public static string? DescribirCambios(params (string Campo, object? Antes, object? Despues)[] campos)
        {
            var cambios = campos
                .Select(c => (c.Campo, Antes: Texto(c.Antes), Despues: Texto(c.Despues)))
                .Where(c => c.Antes != c.Despues)
                .Select(c => $"{c.Campo}: {c.Antes} → {c.Despues}")
                .ToList();

            return cambios.Count == 0 ? null : string.Join("; ", cambios);
        }

        private static string Texto(object? valor) => valor switch
        {
            null => "(vacío)",
            string s when string.IsNullOrWhiteSpace(s) => "(vacío)",
            bool b => b ? "Sí" : "No",
            decimal d => d.ToString("0.####"),
            DateTime f => f.ToString("dd/MM/yyyy"),
            _ => valor.ToString()!.Trim()
        };

        private static string? Recortar(string? texto, int max) =>
            texto == null ? null : texto.Length > max ? texto[..max] : texto;

        public async Task<IEnumerable<AuditoriaCultivo>> GetAuditoria(Guid? cultivoId, string? tipoEntidad, DateTime? desde, DateTime? hasta)
        {
            var query = _context.Auditoria_Cultivo.Include(a => a.Usuario).AsQueryable();

            if (cultivoId.HasValue) query = query.Where(a => a.CultivoId == cultivoId.Value);
            if (!string.IsNullOrWhiteSpace(tipoEntidad)) query = query.Where(a => a.TipoEntidad == tipoEntidad);
            if (desde.HasValue) query = query.Where(a => a.Fecha >= desde.Value);
            if (hasta.HasValue) query = query.Where(a => a.Fecha <= hasta.Value);

            return await query.OrderByDescending(a => a.Fecha).Take(1000).ToListAsync();
        }

        public async Task<IEnumerable<EventoDispositivoCultivo>> GetEventos(Guid? cultivoId, string? tipoDispositivo, DateTime? desde, DateTime? hasta)
        {
            var query = _context.Eventos_Dispositivo_Cultivo.Include(e => e.Usuario).AsQueryable();

            if (cultivoId.HasValue) query = query.Where(e => e.CultivoId == cultivoId.Value);
            if (!string.IsNullOrWhiteSpace(tipoDispositivo)) query = query.Where(e => e.TipoDispositivo == tipoDispositivo);
            if (desde.HasValue) query = query.Where(e => e.Fecha >= desde.Value);
            if (hasta.HasValue) query = query.Where(e => e.Fecha <= hasta.Value);

            return await query.OrderByDescending(e => e.Fecha).Take(1000).ToListAsync();
        }

        public async Task<IEnumerable<BitacoraAlarmaCultivo>> GetBitacoraAlarmas(Guid? cultivoId, DateTime? desde, DateTime? hasta)
        {
            var query = _context.Bitacora_Alarmas_Cultivo
                .Include(b => b.Usuario)
                .Include(b => b.Alarma)
                .AsQueryable();

            if (cultivoId.HasValue) query = query.Where(b => b.Alarma != null && b.Alarma.CultivoId == cultivoId.Value);
            if (desde.HasValue) query = query.Where(b => b.Fecha >= desde.Value);
            if (hasta.HasValue) query = query.Where(b => b.Fecha <= hasta.Value);

            return await query.OrderByDescending(b => b.Fecha).Take(1000).ToListAsync();
        }
    }
}
