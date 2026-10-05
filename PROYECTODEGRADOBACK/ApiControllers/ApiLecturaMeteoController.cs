using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Models.Dto;
using RR_Nueva_Naturaleza.Service;

namespace RR_Nueva_Naturaleza.ApiControllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ApiLecturaMeteoController : ControllerBase
    {
        private readonly ILecturaMeteoService _lecturaMeteoService;
        private readonly IMapper _mapper;

        public ApiLecturaMeteoController(ILecturaMeteoService lecturaMeteoService, IMapper mapper)
        {
            _lecturaMeteoService = lecturaMeteoService;
            _mapper = mapper;
        }

        [Authorize(Roles = "Administrador,Operario,Usuario Externo")]
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<LecturaMeteoDto>), 200)]
        public async Task<IEnumerable<LecturaMeteoDto>> GetLecturas()
        {
            var lecturas = await _lecturaMeteoService.GetLecturas();
            return _mapper.Map<List<LecturaMeteo>, List<LecturaMeteoDto>>(lecturas.ToList());
        }

        [Authorize(Roles = "Administrador,Operario,Usuario Externo")]
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(LecturaMeteoDto), 200)]
        [ProducesResponseType(typeof(string), 404)]
        public async Task<IActionResult> GetLectura(Guid id)
        {
            var lectura = await _lecturaMeteoService.GetLectura(id);
            if (lectura == null)
            {
                return NotFound();
            }
            return Ok(_mapper.Map<LecturaMeteo, LecturaMeteoDto>(lectura));
        }

        [Authorize(Roles = "Administrador,Operario,Usuario Externo")]
        [HttpGet("GetByEstacion/{numero}")]
        [ProducesResponseType(typeof(IEnumerable<LecturaMeteoDto>), 200)]
        public async Task<IEnumerable<LecturaMeteoDto>> GetByEstacion(int numero)
        {
            var lecturas = await _lecturaMeteoService.GetLecturasByEstacion(numero);
            return _mapper.Map<List<LecturaMeteo>, List<LecturaMeteoDto>>(lecturas.ToList());
        }

        [Authorize(Roles = "Administrador,Operario,Usuario Externo")]
        [HttpGet("GetByEstacionAndVariable/{numero}/{variable}")]
        [ProducesResponseType(typeof(IEnumerable<LecturaMeteoDto>), 200)]
        public async Task<IEnumerable<LecturaMeteoDto>> GetByEstacionAndVariable(int numero, string variable)
        {
            var lecturas = await _lecturaMeteoService.GetLecturasByEstacionAndVariable(numero, variable);
            return _mapper.Map<List<LecturaMeteo>, List<LecturaMeteoDto>>(lecturas.ToList());
        }

        [Authorize(Roles = "Administrador,Operario")]
        [HttpGet("Estadisticas/{numero}/{variable}")]
        [ProducesResponseType(typeof(EstadisticaMeteoDto), 200)]
        [ProducesResponseType(typeof(string), 400)]
        public async Task<IActionResult> GetEstadisticas(int numero, string variable, [FromQuery] int cantidad = 100)
        {
            // La dirección del viento es angular: media/mediana aritmética no aplican
            if (variable == "direccion")
            {
                return BadRequest("La dirección del viento no se analiza con estas medidas.");
            }
            if (cantidad < 1 || cantidad > 10000)
            {
                return BadRequest("La cantidad de datos debe estar entre 1 y 10000.");
            }

            var estadisticas = await _lecturaMeteoService.GetEstadisticas(numero, variable, cantidad);
            return Ok(estadisticas);
        }

        [Authorize(Roles = "Administrador")]
        [HttpPost("GetLecturasForPdf")]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        public async Task<IActionResult> GetLecturasForPdf(LecturaMeteoPdfRequestDto filtro)
        {
            using var ms = new MemoryStream();
            await _lecturaMeteoService.GenerarPdf(ms, filtro);
            var fileName = $"Reporte_Meteo_{DateTime.Now:yyyyMMddHHmm}.pdf";
            return File(ms.ToArray(), "application/pdf", fileName);
        }
    }
}
