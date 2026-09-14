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
    public class ApiUmbralMeteoController : ControllerBase
    {
        private readonly IUmbralMeteoService _umbralMeteoService;
        private readonly IMapper _mapper;

        public ApiUmbralMeteoController(IUmbralMeteoService umbralMeteoService, IMapper mapper)
        {
            _umbralMeteoService = umbralMeteoService;
            _mapper = mapper;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<UmbralMeteoDto>), 200)]
        public async Task<IEnumerable<UmbralMeteoDto>> GetUmbrales()
        {
            var umbrales = await _umbralMeteoService.GetUmbrales();
            return _mapper.Map<List<UmbralMeteo>, List<UmbralMeteoDto>>(umbrales.ToList());
        }

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
    }
}
