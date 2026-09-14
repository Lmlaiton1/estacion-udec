using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Service;

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

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<SensorMeteo>), 200)]
        public async Task<IEnumerable<SensorMeteo>> GetSensores()
        {
            return await _sensorMeteoService.GetSensores();
        }

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
    }
}
