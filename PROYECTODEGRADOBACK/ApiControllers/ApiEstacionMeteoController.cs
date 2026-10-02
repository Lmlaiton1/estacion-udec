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
    public class ApiEstacionMeteoController : ControllerBase
    {
        private readonly IEstacionMeteoService _estacionMeteoService;

        public ApiEstacionMeteoController(IEstacionMeteoService estacionMeteoService)
        {
            _estacionMeteoService = estacionMeteoService;
        }

        [Authorize(Roles = "Administrador,Operario,Usuario Externo")]
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<EstacionMeteo>), 200)]
        public async Task<IEnumerable<EstacionMeteo>> GetEstaciones()
        {
            return await _estacionMeteoService.GetEstaciones();
        }

        [Authorize(Roles = "Administrador,Operario,Usuario Externo")]
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(EstacionMeteo), 200)]
        [ProducesResponseType(typeof(string), 404)]
        public async Task<IActionResult> GetEstacion(Guid id)
        {
            var estacion = await _estacionMeteoService.GetEstacion(id);
            if (estacion == null)
            {
                return NotFound();
            }
            return Ok(estacion);
        }

        [HttpPut("CreateEstacion")]
        [Authorize(Roles = "Administrador")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        [ProducesResponseType(typeof(ServiceResponse), 400)]
        public async Task<IActionResult> CreateEstacion(EstacionMeteoCreateDto estacion)
        {
            var response = await _estacionMeteoService.AddEstacion(estacion);
            if (response.Result == ServiceResponseType.Failed)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpPut("UpdateEstacion/{id}")]
        [Authorize(Roles = "Administrador")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        [ProducesResponseType(typeof(ServiceResponse), 400)]
        public async Task<IActionResult> UpdateEstacion(Guid id, EstacionMeteoUpdateDto estacion)
        {
            var response = await _estacionMeteoService.UpdateEstacion(id, estacion);
            if (response.Result == ServiceResponseType.Failed)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        [ProducesResponseType(typeof(ServiceResponse), 400)]
        public async Task<IActionResult> DeleteEstacion(Guid id)
        {
            var response = await _estacionMeteoService.DeleteEstacion(id);
            if (response.Result == ServiceResponseType.Failed)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpPut("CambiarEstado/{id}")]
        [Authorize(Roles = "Administrador")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        [ProducesResponseType(typeof(ServiceResponse), 400)]
        public async Task<IActionResult> CambiarEstado(Guid id, CambiarEstadoDto cambio)
        {
            var usuarioId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var response = await _estacionMeteoService.CambiarEstado(id, cambio, usuarioId);
            if (response.Result == ServiceResponseType.Failed)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
    }
}
