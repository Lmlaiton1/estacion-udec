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
    public class ApiAlertaMeteoController : ControllerBase
    {
        private readonly IAlertaMeteoService _alertaMeteoService;
        private readonly IMapper _mapper;

        public ApiAlertaMeteoController(IAlertaMeteoService alertaMeteoService, IMapper mapper)
        {
            _alertaMeteoService = alertaMeteoService;
            _mapper = mapper;
        }

        [Authorize(Roles = "Administrador")]
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<AlertaMeteoDto>), 200)]
        public async Task<IEnumerable<AlertaMeteoDto>> GetAlertas()
        {
            var alertas = await _alertaMeteoService.GetAlertas();
            return _mapper.Map<List<AlertaMeteo>, List<AlertaMeteoDto>>(alertas.ToList());
        }

        [Authorize(Roles = "Administrador")]
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

        [Authorize(Roles = "Administrador")]
        [HttpPut("MarcarVista/{id}")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        [ProducesResponseType(typeof(ServiceResponse), 400)]
        public async Task<IActionResult> MarcarVista(Guid id, MarcarVistaAlertaDto marcar)
        {
            var usuarioId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var response = await _alertaMeteoService.MarcarVista(id, marcar.AccionTomada, usuarioId);
            if (response.Result == ServiceResponseType.Failed)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
    }
}
