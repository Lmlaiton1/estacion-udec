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
    public class ApiActuadorCultivoController : ControllerBase
    {
        private readonly IActuadorCultivoService _actuadorCultivoService;
        private readonly IMapper _mapper;

        public ApiActuadorCultivoController(IActuadorCultivoService actuadorCultivoService, IMapper mapper)
        {
            _actuadorCultivoService = actuadorCultivoService;
            _mapper = mapper;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ActuadorCultivoDto>), 200)]
        public async Task<IEnumerable<ActuadorCultivoDto>> GetActuadores([FromQuery] Guid? cultivoId)
        {
            var actuadores = await _actuadorCultivoService.GetActuadores(cultivoId);
            return _mapper.Map<List<ActuadorCultivo>, List<ActuadorCultivoDto>>(actuadores.ToList());
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ActuadorCultivoDto), 200)]
        [ProducesResponseType(typeof(string), 404)]
        public async Task<IActionResult> GetActuador(Guid id)
        {
            var actuador = await _actuadorCultivoService.GetActuador(id);
            if (actuador == null)
            {
                return NotFound();
            }
            return Ok(_mapper.Map<ActuadorCultivo, ActuadorCultivoDto>(actuador));
        }

        [Authorize(Roles = "Administrador")]
        [HttpPut("CreateActuador")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        [ProducesResponseType(typeof(ServiceResponse), 400)]
        public async Task<IActionResult> CreateActuador(ActuadorCultivoCreateDto actuador)
        {
            return Responder(await _actuadorCultivoService.AddActuador(actuador, UsuarioActual()));
        }

        [Authorize(Roles = "Administrador")]
        [HttpPut("UpdateActuador/{id}")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        [ProducesResponseType(typeof(ServiceResponse), 400)]
        public async Task<IActionResult> UpdateActuador(Guid id, ActuadorCultivoCreateDto actuador)
        {
            return Responder(await _actuadorCultivoService.UpdateActuador(id, actuador, UsuarioActual()));
        }

        [Authorize(Roles = "Administrador")]
        [HttpPut("Eliminar/{id}")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        [ProducesResponseType(typeof(ServiceResponse), 400)]
        public async Task<IActionResult> DeleteActuador(Guid id, MotivoDto motivo)
        {
            return Responder(await _actuadorCultivoService.DeleteActuador(id, motivo.Motivo, UsuarioActual()));
        }

        // Encendido/apagado manual: Administrador y Operario
        [HttpPut("Accionar/{id}")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        [ProducesResponseType(typeof(ServiceResponse), 400)]
        public async Task<IActionResult> Accionar(Guid id, AccionarActuadorDto accion)
        {
            return Responder(await _actuadorCultivoService.Accionar(id, accion, UsuarioActual()));
        }

        [HttpPut("CambiarModo/{id}")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        [ProducesResponseType(typeof(ServiceResponse), 400)]
        public async Task<IActionResult> CambiarModo(Guid id, CambiarModoActuadorDto cambio)
        {
            return Responder(await _actuadorCultivoService.CambiarModo(id, cambio, UsuarioActual()));
        }

        [Authorize(Roles = "Administrador")]
        [HttpPut("CambiarEstado/{id}")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        [ProducesResponseType(typeof(ServiceResponse), 400)]
        public async Task<IActionResult> CambiarEstado(Guid id, CambiarEstadoDto cambio)
        {
            return Responder(await _actuadorCultivoService.CambiarEstado(id, cambio, UsuarioActual()));
        }

        private Guid UsuarioActual() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        private IActionResult Responder(ServiceResponse response) =>
            response.Result == ServiceResponseType.Failed ? BadRequest(response) : Ok(response);
    }
}
