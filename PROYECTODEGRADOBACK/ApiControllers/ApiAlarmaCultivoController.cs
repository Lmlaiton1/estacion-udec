using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Models.Dto;
using RR_Nueva_Naturaleza.Service;
using System.Security.Claims;

namespace RR_Nueva_Naturaleza.ApiControllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Administrador,Operario")]
    public class ApiAlarmaCultivoController : ControllerBase
    {
        private readonly IAlarmaCultivoService _alarmaCultivoService;
        private readonly IMapper _mapper;

        public ApiAlarmaCultivoController(IAlarmaCultivoService alarmaCultivoService, IMapper mapper)
        {
            _alarmaCultivoService = alarmaCultivoService;
            _mapper = mapper;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<AlarmaCultivoDto>), 200)]
        public async Task<IEnumerable<AlarmaCultivoDto>> GetAlarmas([FromQuery] Guid? cultivoId, [FromQuery] bool soloAbiertas = false,
            [FromQuery] DateTime? desde = null, [FromQuery] DateTime? hasta = null)
        {
            var alarmas = await _alarmaCultivoService.GetAlarmas(cultivoId, soloAbiertas, desde, hasta);
            return _mapper.Map<List<AlarmaCultivo>, List<AlarmaCultivoDto>>(alarmas.ToList());
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(AlarmaCultivoDto), 200)]
        [ProducesResponseType(typeof(string), 404)]
        public async Task<IActionResult> GetAlarma(Guid id)
        {
            var alarma = await _alarmaCultivoService.GetAlarma(id);
            if (alarma == null)
            {
                return NotFound();
            }
            return Ok(_mapper.Map<AlarmaCultivo, AlarmaCultivoDto>(alarma));
        }

        [HttpPut("MarcarVista/{id}")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        [ProducesResponseType(typeof(ServiceResponse), 400)]
        public async Task<IActionResult> MarcarVista(Guid id)
        {
            var response = await _alarmaCultivoService.MarcarVista(id, UsuarioActual());
            if (response.Result == ServiceResponseType.Failed)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpPut("TomarControl/{id}")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        [ProducesResponseType(typeof(ServiceResponse), 400)]
        public async Task<IActionResult> TomarControl(Guid id, AccionAlarmaDto accion)
        {
            var response = await _alarmaCultivoService.TomarControl(id, accion.Comentario, UsuarioActual());
            if (response.Result == ServiceResponseType.Failed)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpPut("Comentar/{id}")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        [ProducesResponseType(typeof(ServiceResponse), 400)]
        public async Task<IActionResult> Comentar(Guid id, AccionAlarmaDto accion)
        {
            var response = await _alarmaCultivoService.Comentar(id, accion.Comentario, UsuarioActual());
            if (response.Result == ServiceResponseType.Failed)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpPut("Resolver/{id}")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        [ProducesResponseType(typeof(ServiceResponse), 400)]
        public async Task<IActionResult> Resolver(Guid id, AccionAlarmaDto accion)
        {
            var response = await _alarmaCultivoService.Resolver(id, accion.Comentario, UsuarioActual());
            if (response.Result == ServiceResponseType.Failed)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        // Administración de alarmas: solo Administrador, siempre con motivo
        [Authorize(Roles = "Administrador")]
        [HttpPut("UpdateAlarma/{id}")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        [ProducesResponseType(typeof(ServiceResponse), 400)]
        public async Task<IActionResult> UpdateAlarma(Guid id, AlarmaCultivoUpdateDto cambio)
        {
            return Responder(await _alarmaCultivoService.UpdateAlarma(id, cambio, UsuarioActual()));
        }

        [Authorize(Roles = "Administrador")]
        [HttpPut("Anular/{id}")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        [ProducesResponseType(typeof(ServiceResponse), 400)]
        public async Task<IActionResult> Anular(Guid id, MotivoDto motivo)
        {
            return Responder(await _alarmaCultivoService.Anular(id, motivo.Motivo, UsuarioActual()));
        }

        [Authorize(Roles = "Administrador")]
        [HttpPut("Reactivar/{id}")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        [ProducesResponseType(typeof(ServiceResponse), 400)]
        public async Task<IActionResult> Reactivar(Guid id, MotivoDto motivo)
        {
            return Responder(await _alarmaCultivoService.Reactivar(id, motivo.Motivo, UsuarioActual()));
        }

        [Authorize(Roles = "Administrador")]
        [HttpPut("Eliminar/{id}")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        [ProducesResponseType(typeof(ServiceResponse), 400)]
        public async Task<IActionResult> DeleteAlarma(Guid id, MotivoDto motivo)
        {
            return Responder(await _alarmaCultivoService.DeleteAlarma(id, motivo.Motivo, UsuarioActual()));
        }

        private IActionResult Responder(ServiceResponse response) =>
            response.Result == ServiceResponseType.Failed ? BadRequest(response) : Ok(response);

        private Guid UsuarioActual() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }
}
