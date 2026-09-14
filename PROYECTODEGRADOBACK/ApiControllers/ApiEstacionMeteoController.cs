using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Service;

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

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<EstacionMeteo>), 200)]
        public async Task<IEnumerable<EstacionMeteo>> GetEstaciones()
        {
            return await _estacionMeteoService.GetEstaciones();
        }

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
    }
}
