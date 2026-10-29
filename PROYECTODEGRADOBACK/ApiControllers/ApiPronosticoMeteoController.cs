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
    public class ApiPronosticoMeteoController : ControllerBase
    {
        private readonly IPronosticoMeteoService _pronosticoMeteoService;

        public ApiPronosticoMeteoController(IPronosticoMeteoService pronosticoMeteoService)
        {
            _pronosticoMeteoService = pronosticoMeteoService;
        }

        // Botón "Pronóstico del día": reentrena con las lecturas recientes y pronostica el día siguiente
        [Authorize(Roles = "Administrador,Operario")]
        [HttpPost("Generar")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        [ProducesResponseType(typeof(ServiceResponse), 400)]
        public async Task<IActionResult> Generar()
        {
            var response = await _pronosticoMeteoService.GenerarPronostico(UsuarioActual());
            if (response.Result == ServiceResponseType.Failed)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet("Ultimo")]
        [ProducesResponseType(typeof(PronosticoMeteoDto), 200)]
        [ProducesResponseType(204)]
        public async Task<IActionResult> GetUltimo()
        {
            var pronostico = await _pronosticoMeteoService.GetUltimo();
            if (pronostico == null)
            {
                return NoContent();
            }
            return Ok(pronostico);
        }

        // Franjas ya transcurridas de los últimos días, para calificarlas
        [HttpGet("Franjas")]
        [ProducesResponseType(typeof(IEnumerable<PronosticoFranjaMeteoDto>), 200)]
        public async Task<IEnumerable<PronosticoFranjaMeteoDto>> GetFranjas([FromQuery] int dias = 7)
        {
            return await _pronosticoMeteoService.GetFranjasRecientes(dias);
        }

        [HttpPost("Calificar")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        [ProducesResponseType(typeof(ServiceResponse), 400)]
        public async Task<IActionResult> Calificar(RetroalimentacionMeteoCreateDto retro)
        {
            var response = await _pronosticoMeteoService.Calificar(retro, UsuarioActual());
            if (response.Result == ServiceResponseType.Failed)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet("Estadisticas")]
        [ProducesResponseType(typeof(EstadisticaPronosticoDto), 200)]
        public async Task<EstadisticaPronosticoDto> GetEstadisticas()
        {
            return await _pronosticoMeteoService.GetEstadisticas();
        }

        private Guid UsuarioActual() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }
}
