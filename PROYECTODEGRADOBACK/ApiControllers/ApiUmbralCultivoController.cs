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
    public class ApiUmbralCultivoController : ControllerBase
    {
        private readonly IUmbralCultivoService _umbralCultivoService;
        private readonly IMapper _mapper;

        public ApiUmbralCultivoController(IUmbralCultivoService umbralCultivoService, IMapper mapper)
        {
            _umbralCultivoService = umbralCultivoService;
            _mapper = mapper;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<UmbralCultivoDto>), 200)]
        public async Task<IEnumerable<UmbralCultivoDto>> GetUmbrales([FromQuery] Guid? cultivoId)
        {
            var umbrales = await _umbralCultivoService.GetUmbrales(cultivoId);
            return _mapper.Map<List<UmbralCultivo>, List<UmbralCultivoDto>>(umbrales.ToList());
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(UmbralCultivoDto), 200)]
        [ProducesResponseType(typeof(string), 404)]
        public async Task<IActionResult> GetUmbral(Guid id)
        {
            var umbral = await _umbralCultivoService.GetUmbral(id);
            if (umbral == null)
            {
                return NotFound();
            }
            return Ok(_mapper.Map<UmbralCultivo, UmbralCultivoDto>(umbral));
        }

        [Authorize(Roles = "Administrador")]
        [HttpPut("CreateUmbral")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        [ProducesResponseType(typeof(ServiceResponse), 400)]
        public async Task<IActionResult> CreateUmbral(UmbralCultivoCreateDto umbral)
        {
            return Responder(await _umbralCultivoService.AddUmbral(umbral, UsuarioActual()));
        }

        [Authorize(Roles = "Administrador")]
        [HttpPut("UpdateUmbral/{id}")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        [ProducesResponseType(typeof(ServiceResponse), 400)]
        public async Task<IActionResult> UpdateUmbral(Guid id, UmbralCultivoCreateDto umbral)
        {
            return Responder(await _umbralCultivoService.UpdateUmbral(id, umbral, UsuarioActual()));
        }

        [Authorize(Roles = "Administrador")]
        [HttpPut("CambiarEstado/{id}")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        [ProducesResponseType(typeof(ServiceResponse), 400)]
        public async Task<IActionResult> CambiarEstado(Guid id, CambiarEstadoDto cambio)
        {
            return Responder(await _umbralCultivoService.CambiarEstado(id, cambio, UsuarioActual()));
        }

        [Authorize(Roles = "Administrador")]
        [HttpPut("Eliminar/{id}")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        [ProducesResponseType(typeof(ServiceResponse), 400)]
        public async Task<IActionResult> DeleteUmbral(Guid id, MotivoDto motivo)
        {
            return Responder(await _umbralCultivoService.DeleteUmbral(id, motivo.Motivo, UsuarioActual()));
        }

        private Guid UsuarioActual() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        private IActionResult Responder(ServiceResponse response) =>
            response.Result == ServiceResponseType.Failed ? BadRequest(response) : Ok(response);
    }
}
