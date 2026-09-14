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
    public class ApiAlertaMeteoController : ControllerBase
    {
        private readonly IAlertaMeteoService _alertaMeteoService;
        private readonly IMapper _mapper;

        public ApiAlertaMeteoController(IAlertaMeteoService alertaMeteoService, IMapper mapper)
        {
            _alertaMeteoService = alertaMeteoService;
            _mapper = mapper;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<AlertaMeteoDto>), 200)]
        public async Task<IEnumerable<AlertaMeteoDto>> GetAlertas()
        {
            var alertas = await _alertaMeteoService.GetAlertas();
            return _mapper.Map<List<AlertaMeteo>, List<AlertaMeteoDto>>(alertas.ToList());
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(AlertaMeteoDto), 200)]
        [ProducesResponseType(typeof(string), 404)]
        public async Task<IActionResult> GetAlerta(Guid id)
        {
            var alerta = await _alertaMeteoService.GetAlerta(id);
            if (alerta == null)
            {
                return NotFound();
            }
            return Ok(_mapper.Map<AlertaMeteo, AlertaMeteoDto>(alerta));
        }
    }
}
