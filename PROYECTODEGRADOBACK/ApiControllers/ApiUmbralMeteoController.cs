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
    [Authorize]
    public class ApiUmbralMeteoController : ControllerBase
    {
        private readonly IUmbralMeteoService _umbralMeteoService;
        private readonly IMapper _mapper;

        public ApiUmbralMeteoController(IUmbralMeteoService umbralMeteoService, IMapper mapper)
        {
            _umbralMeteoService = umbralMeteoService;
            _mapper = mapper;
        }

        [Authorize(Roles = "Administrador")]
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<UmbralMeteoDto>), 200)]
        public async Task<IEnumerable<UmbralMeteoDto>> GetUmbrales()
        {
            var umbrales = await _umbralMeteoService.GetUmbrales();
            return _mapper.Map<List<UmbralMeteo>, List<UmbralMeteoDto>>(umbrales.ToList());
        }

        [Authorize(Roles = "Administrador")]
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(UmbralMeteoDto), 200)]
        [ProducesResponseType(typeof(string), 404)]
        public async Task<IActionResult> GetUmbral(Guid id)
        {
            var umbral = await _umbralMeteoService.GetUmbral(id);
            if (umbral == null)
            {
                return NotFound();
            }
            return Ok(_mapper.Map<UmbralMeteo, UmbralMeteoDto>(umbral));
        }

        [Authorize(Roles = "Administrador")]
        [HttpPut("CreateUmbral")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        [ProducesResponseType(typeof(ServiceResponse), 400)]
        public async Task<IActionResult> CreateUmbral(UmbralMeteoCreateDto umbral)
        {
            var response = await _umbralMeteoService.AddUmbral(umbral, UsuarioActual());
            if (response.Result == ServiceResponseType.Failed)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [Authorize(Roles = "Administrador")]
        [HttpPut("UpdateUmbral/{id}")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        [ProducesResponseType(typeof(ServiceResponse), 400)]
        public async Task<IActionResult> UpdateUmbral(Guid id, UmbralMeteoUpdateDto umbral)
        {
            var response = await _umbralMeteoService.UpdateUmbral(id, umbral, UsuarioActual());
            if (response.Result == ServiceResponseType.Failed)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [Authorize(Roles = "Administrador")]
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        [ProducesResponseType(typeof(ServiceResponse), 400)]
        public async Task<IActionResult> DeleteUmbral(Guid id, [FromBody] MotivoDto motivo)
        {
            var response = await _umbralMeteoService.DeleteUmbral(id, motivo.Motivo, UsuarioActual());
            if (response.Result == ServiceResponseType.Failed)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        private Guid UsuarioActual() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }
}
