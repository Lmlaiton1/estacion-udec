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

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<LecturaMeteoDto>), 200)]
        public async Task<IEnumerable<LecturaMeteoDto>> GetLecturas()
        {
            var lecturas = await _lecturaMeteoService.GetLecturas();
            return _mapper.Map<List<LecturaMeteo>, List<LecturaMeteoDto>>(lecturas.ToList());
        }

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

        [HttpGet("GetByEstacion/{numero}")]
        [ProducesResponseType(typeof(IEnumerable<LecturaMeteoDto>), 200)]
        public async Task<IEnumerable<LecturaMeteoDto>> GetByEstacion(int numero)
        {
            var lecturas = await _lecturaMeteoService.GetLecturasByEstacion(numero);
            return _mapper.Map<List<LecturaMeteo>, List<LecturaMeteoDto>>(lecturas.ToList());
        }

        [HttpGet("GetByEstacionAndVariable/{numero}/{variable}")]
        [ProducesResponseType(typeof(IEnumerable<LecturaMeteoDto>), 200)]
        public async Task<IEnumerable<LecturaMeteoDto>> GetByEstacionAndVariable(int numero, string variable)
        {
            var lecturas = await _lecturaMeteoService.GetLecturasByEstacionAndVariable(numero, variable);
            return _mapper.Map<List<LecturaMeteo>, List<LecturaMeteoDto>>(lecturas.ToList());
        }
    }
}
