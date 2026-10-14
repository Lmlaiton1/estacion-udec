using Microsoft.EntityFrameworkCore;
using RR_Nueva_Naturaleza.DAL;
using RR_Nueva_Naturaleza.Models;

namespace RR_Nueva_Naturaleza.Service
{
    public class AuditoriaMeteoService : IAuditoriaMeteoService
    {
        private readonly RR_Nueva_NaturalezaContext _context;

        public AuditoriaMeteoService(RR_Nueva_NaturalezaContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<AuditoriaMeteo>> GetAuditoria()
        {
            return await _context.Auditoria_Meteo
                .Include(a => a.Usuario)
                .OrderByDescending(a => a.Fecha)
                .ToListAsync();
        }

        public Task Registrar(string tipoEntidad, Guid entidadId, string nombreEntidad, bool nuevoEstado, string motivo, Guid usuarioId)
        {
            return RegistrarAccion(tipoEntidad, entidadId, nombreEntidad, nuevoEstado ? "Activada" : "Desactivada", motivo, usuarioId);
        }

        // Acciones que no son activar/desactivar (ej. "Reiniciada")
        public async Task RegistrarAccion(string tipoEntidad, Guid entidadId, string nombreEntidad, string accion, string motivo, Guid usuarioId)
        {
            _context.Auditoria_Meteo.Add(new AuditoriaMeteo
            {
                Id = Guid.NewGuid(),
                TipoEntidad = tipoEntidad,
                EntidadId = entidadId,
                NombreEntidad = nombreEntidad,
                Accion = accion,
                Motivo = motivo,
                Fecha = DateTime.Now,
                UsuarioId = usuarioId
            });
            await _context.SaveChangesAsync();
        }
    }
}
