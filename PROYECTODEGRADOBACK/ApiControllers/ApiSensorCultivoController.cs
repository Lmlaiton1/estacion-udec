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
    public class ApiSensorCultivoController : ControllerBase
    {
        private readonly ISensorCultivoService _sensorCultivoService;
        private readonly IMapper _mapper;

        public ApiSensorCultivoController(ISensorCultivoService sensorCultivoService, IMapper mapper)
        {
            _sensorCultivoService = sensorCultivoService;
            _mapper = mapper;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<SensorCultivoDto>), 200)]
        public async Task<IEnumerable<SensorCultivoDto>> GetSensores([FromQuery] Guid? cultivoId)
        {
            return await _sensorCultivoService.GetSensores(cultivoId);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(SensorCultivoDto), 200)]
        [ProducesResponseType(typeof(string), 404)]
        public async Task<IActionResult> GetSensor(Guid id)
        {
            var sensor = await _sensorCultivoService.GetSensor(id);
            if (sensor == null)
            {
                return NotFound();
            }
            return Ok(_mapper.Map<SensorCultivo, SensorCultivoDto>(sensor));
        }

        [HttpGet("Lecturas/{id}")]
        [ProducesResponseType(typeof(IEnumerable<LecturaCultivoDto>), 200)]
        public async Task<IEnumerable<LecturaCultivoDto>> GetLecturas(Guid id, [FromQuery] int horas = 24)
        {
            var lecturas = await _sensorCultivoService.GetLecturas(id, horas);
            return _mapper.Map<List<LecturaCultivo>, List<LecturaCultivoDto>>(lecturas.ToList());
        }

        [Authorize(Roles = "Administrador")]
        [HttpPut("CreateSensor")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        [ProducesResponseType(typeof(ServiceResponse), 400)]
        public async Task<IActionResult> CreateSensor(SensorCultivoCreateDto sensor)
        {
            return Responder(await _sensorCultivoService.AddSensor(sensor, UsuarioActual()));
        }

        [Authorize(Roles = "Administrador")]
        [HttpPut("UpdateSensor/{id}")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        [ProducesResponseType(typeof(ServiceResponse), 400)]
        public async Task<IActionResult> UpdateSensor(Guid id, SensorCultivoCreateDto sensor)
        {
            return Responder(await _sensorCultivoService.UpdateSensor(id, sensor, UsuarioActual()));
        }

        [Authorize(Roles = "Administrador")]
        [HttpPut("Eliminar/{id}")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        [ProducesResponseType(typeof(ServiceResponse), 400)]
        public async Task<IActionResult> DeleteSensor(Guid id, MotivoDto motivo)
        {
            return Responder(await _sensorCultivoService.DeleteSensor(id, motivo.Motivo, UsuarioActual()));
        }

        [Authorize(Roles = "Administrador")]
        [HttpPut("CambiarEstado/{id}")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        [ProducesResponseType(typeof(ServiceResponse), 400)]
        public async Task<IActionResult> CambiarEstado(Guid id, CambiarEstadoDto cambio)
        {
            return Responder(await _sensorCultivoService.CambiarEstado(id, cambio, UsuarioActual()));
        }

        private Guid UsuarioActual() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        private IActionResult Responder(ServiceResponse response) =>
            response.Result == ServiceResponseType.Failed ? BadRequest(response) : Ok(response);
    }
}
