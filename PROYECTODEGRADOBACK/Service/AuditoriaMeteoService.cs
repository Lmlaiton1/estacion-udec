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

        public void Agregar(string tipoEntidad, Guid entidadId, string nombreEntidad, string accion, string? detalle, string motivo, Guid usuarioId)
        {
            _context.Auditoria_Meteo.Add(new AuditoriaMeteo
            {
                Id = Guid.NewGuid(),
                TipoEntidad = tipoEntidad,
                EntidadId = entidadId,
                NombreEntidad = nombreEntidad,
                Accion = accion,
                Detalle = detalle,
                Motivo = motivo,
                Fecha = DateTime.Now,
                UsuarioId = usuarioId
            });
        }

        public async Task Registrar(string tipoEntidad, Guid entidadId, string nombreEntidad, bool nuevoEstado, string motivo, Guid usuarioId)
        {
            _context.Auditoria_Meteo.Add(new AuditoriaMeteo
            {
                Id = Guid.NewGuid(),
                TipoEntidad = tipoEntidad,
                EntidadId = entidadId,
                NombreEntidad = nombreEntidad,
                Accion = nuevoEstado ? "Activada" : "Desactivada",
                Motivo = motivo,
                Fecha = DateTime.Now,
                UsuarioId = usuarioId
            });
            await _context.SaveChangesAsync();
        }
    }
}
