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
    public class ApiCultivoController : ControllerBase
    {
        private readonly ICultivoService _cultivoService;
        private readonly IMapper _mapper;

        public ApiCultivoController(ICultivoService cultivoService, IMapper mapper)
        {
            _cultivoService = cultivoService;
            _mapper = mapper;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<CultivoDto>), 200)]
        public async Task<IEnumerable<CultivoDto>> GetCultivos()
        {
            var cultivos = await _cultivoService.GetCultivos();
            return _mapper.Map<List<Cultivo>, List<CultivoDto>>(cultivos.ToList());
        }

        // Sensores y actuadores de acuaponía (Nueva Naturaleza) que se pueden vincular a un cultivo
        [HttpGet("DispositivosNuevaNaturaleza")]
        [ProducesResponseType(typeof(IEnumerable<DispositivoNuevaNaturalezaDto>), 200)]
        public async Task<IEnumerable<DispositivoNuevaNaturalezaDto>> GetDispositivosNuevaNaturaleza()
        {
            return await _cultivoService.GetDispositivosNuevaNaturaleza();
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(CultivoDto), 200)]
        [ProducesResponseType(typeof(string), 404)]
        public async Task<IActionResult> GetCultivo(Guid id)
        {
            var cultivo = await _cultivoService.GetCultivo(id);
            if (cultivo == null)
            {
                return NotFound();
            }
            return Ok(_mapper.Map<Cultivo, CultivoDto>(cultivo));
        }

        [Authorize(Roles = "Administrador")]
        [HttpPut("CreateCultivo")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        [ProducesResponseType(typeof(ServiceResponse), 400)]
        public async Task<IActionResult> CreateCultivo(CultivoCreateDto cultivo)
        {
            return Responder(await _cultivoService.AddCultivo(cultivo, UsuarioActual()));
        }

        [Authorize(Roles = "Administrador")]
        [HttpPut("UpdateCultivo/{id}")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        [ProducesResponseType(typeof(ServiceResponse), 400)]
        public async Task<IActionResult> UpdateCultivo(Guid id, CultivoCreateDto cultivo)
        {
            return Responder(await _cultivoService.UpdateCultivo(id, cultivo, UsuarioActual()));
        }

        [Authorize(Roles = "Administrador")]
        [HttpPut("CambiarEstado/{id}")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        [ProducesResponseType(typeof(ServiceResponse), 400)]
        public async Task<IActionResult> CambiarEstado(Guid id, CambiarEstadoDto cambio)
        {
            return Responder(await _cultivoService.CambiarEstado(id, cambio, UsuarioActual()));
        }

        [Authorize(Roles = "Administrador")]
        [HttpPut("Eliminar/{id}")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        [ProducesResponseType(typeof(ServiceResponse), 400)]
        public async Task<IActionResult> DeleteCultivo(Guid id, MotivoDto motivo)
        {
            return Responder(await _cultivoService.DeleteCultivo(id, motivo.Motivo, UsuarioActual()));
        }

        // Etapas productivas
        [HttpGet("Etapas/{cultivoId}")]
        [ProducesResponseType(typeof(IEnumerable<EtapaCultivoDto>), 200)]
        public async Task<IEnumerable<EtapaCultivoDto>> GetEtapas(Guid cultivoId)
        {
            var etapas = await _cultivoService.GetEtapas(cultivoId);
            return _mapper.Map<List<EtapaCultivo>, List<EtapaCultivoDto>>(etapas.ToList());
        }

        [Authorize(Roles = "Administrador")]
        [HttpPut("CreateEtapa")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        [ProducesResponseType(typeof(ServiceResponse), 400)]
        public async Task<IActionResult> CreateEtapa(EtapaCultivoCreateDto etapa)
        {
            return Responder(await _cultivoService.AddEtapa(etapa, UsuarioActual()));
        }

        [Authorize(Roles = "Administrador")]
        [HttpPut("UpdateEtapa/{id}")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        [ProducesResponseType(typeof(ServiceResponse), 400)]
        public async Task<IActionResult> UpdateEtapa(Guid id, EtapaCultivoCreateDto etapa)
        {
            return Responder(await _cultivoService.UpdateEtapa(id, etapa, UsuarioActual()));
        }

        [Authorize(Roles = "Administrador")]
        [HttpPut("EliminarEtapa/{id}")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        [ProducesResponseType(typeof(ServiceResponse), 400)]
        public async Task<IActionResult> DeleteEtapa(Guid id, MotivoDto motivo)
        {
            return Responder(await _cultivoService.DeleteEtapa(id, motivo.Motivo, UsuarioActual()));
        }

        // Activar = pasar el cultivo a esta etapa
        [Authorize(Roles = "Administrador")]
        [HttpPut("ActivarEtapa/{id}")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        [ProducesResponseType(typeof(ServiceResponse), 400)]
        public async Task<IActionResult> ActivarEtapa(Guid id, MotivoDto motivo)
        {
            return Responder(await _cultivoService.ActivarEtapa(id, motivo.Motivo, UsuarioActual()));
        }

        // Desactivar = el cultivo queda sin etapa actual
        [Authorize(Roles = "Administrador")]
        [HttpPut("QuitarEtapaActual/{id}")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        [ProducesResponseType(typeof(ServiceResponse), 400)]
        public async Task<IActionResult> QuitarEtapaActual(Guid id, MotivoDto motivo)
        {
            return Responder(await _cultivoService.QuitarEtapaActual(id, motivo.Motivo, UsuarioActual()));
        }

        private Guid UsuarioActual() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        private IActionResult Responder(ServiceResponse response) =>
            response.Result == ServiceResponseType.Failed ? BadRequest(response) : Ok(response);
    }
}
