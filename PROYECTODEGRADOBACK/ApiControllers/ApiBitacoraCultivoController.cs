using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Models.Dto;
using RR_Nueva_Naturaleza.Service;

namespace RR_Nueva_Naturaleza.ApiControllers
{
    // Auditoría del control de cultivo: eventos de sensores/actuadores y bitácora de alarmas
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Administrador,Operario")]
    public class ApiBitacoraCultivoController : ControllerBase
    {
        private readonly IBitacoraCultivoService _bitacoraCultivoService;
        private readonly IMapper _mapper;

        public ApiBitacoraCultivoController(IBitacoraCultivoService bitacoraCultivoService, IMapper mapper)
        {
            _bitacoraCultivoService = bitacoraCultivoService;
            _mapper = mapper;
        }

        [HttpGet("Eventos")]
        [ProducesResponseType(typeof(IEnumerable<EventoDispositivoCultivoDto>), 200)]
        public async Task<IEnumerable<EventoDispositivoCultivoDto>> GetEventos([FromQuery] Guid? cultivoId, [FromQuery] string? tipoDispositivo,
            [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
        {
            var eventos = await _bitacoraCultivoService.GetEventos(cultivoId, tipoDispositivo, desde, hasta);
            return _mapper.Map<List<EventoDispositivoCultivo>, List<EventoDispositivoCultivoDto>>(eventos.ToList());
        }

        // Quién creó, modificó, eliminó, desactivó o reactivó cada elemento del cultivo
        [HttpGet("Configuracion")]
        [ProducesResponseType(typeof(IEnumerable<AuditoriaCultivoDto>), 200)]
        public async Task<IEnumerable<AuditoriaCultivoDto>> GetAuditoria([FromQuery] Guid? cultivoId, [FromQuery] string? tipoEntidad,
            [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
        {
            var auditoria = await _bitacoraCultivoService.GetAuditoria(cultivoId, tipoEntidad, desde, hasta);
            return _mapper.Map<List<AuditoriaCultivo>, List<AuditoriaCultivoDto>>(auditoria.ToList());
        }

        [HttpGet("Alarmas")]
        [ProducesResponseType(typeof(IEnumerable<BitacoraAlarmaCultivoDto>), 200)]
        public async Task<IEnumerable<BitacoraAlarmaCultivoDto>> GetBitacoraAlarmas([FromQuery] Guid? cultivoId,
            [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
        {
            var bitacora = await _bitacoraCultivoService.GetBitacoraAlarmas(cultivoId, desde, hasta);
            return _mapper.Map<List<BitacoraAlarmaCultivo>, List<BitacoraAlarmaCultivoDto>>(bitacora.ToList());
        }
    }
}
