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
    public class ApiSensorMeteoController : ControllerBase
    {
        private readonly ISensorMeteoService _sensorMeteoService;

        public ApiSensorMeteoController(ISensorMeteoService sensorMeteoService)
        {
            _sensorMeteoService = sensorMeteoService;
        }

        [Authorize(Roles = "Administrador,Operario")]
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<SensorMeteo>), 200)]
        public async Task<IEnumerable<SensorMeteo>> GetSensores()
        {
            return await _sensorMeteoService.GetSensores();
        }

        [Authorize(Roles = "Administrador,Operario")]
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(SensorMeteo), 200)]
        [ProducesResponseType(typeof(string), 404)]
        public async Task<IActionResult> GetSensor(Guid id)
        {
            var sensor = await _sensorMeteoService.GetSensor(id);
            if (sensor == null)
            {
                return NotFound();
            }
            return Ok(sensor);
        }

        [HttpPut("CreateSensor")]
        [Authorize(Roles = "Administrador")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        [ProducesResponseType(typeof(ServiceResponse), 400)]
        public async Task<IActionResult> CreateSensor(SensorMeteoCreateDto sensor)
        {
            var response = await _sensorMeteoService.AddSensor(sensor);
            if (response.Result == ServiceResponseType.Failed)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpPut("UpdateSensor/{id}")]
        [Authorize(Roles = "Administrador")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        [ProducesResponseType(typeof(ServiceResponse), 400)]
        public async Task<IActionResult> UpdateSensor(Guid id, SensorMeteoUpdateDto sensor)
        {
            var response = await _sensorMeteoService.UpdateSensor(id, sensor);
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
        public async Task<IActionResult> DeleteSensor(Guid id)
        {
            var response = await _sensorMeteoService.DeleteSensor(id);
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
            var response = await _sensorMeteoService.CambiarEstado(id, cambio, usuarioId);
            if (response.Result == ServiceResponseType.Failed)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
    }
}
