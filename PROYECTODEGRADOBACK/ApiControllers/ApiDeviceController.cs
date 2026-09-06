using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Models.Dto;
using RR_Nueva_Naturaleza.Models.Request;
using RR_Nueva_Naturaleza.Service;
namespace RR_Nueva_Naturaleza.ApiControllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ApiDeviceController : ControllerBase
    {
        private readonly IDeviceService _deviceService;
        private readonly IDevice_StatusService _device_StatusService;
        private readonly ISystem_Type _system_Type;
        private readonly IMapper _mapper;
        private readonly IUnit_MeasurementService _unit_MeasurementService;
        public ApiDeviceController(IDeviceService deviceService, IMapper mapper, IDevice_StatusService device_StatusService, ISystem_Type system_Type, IUnit_MeasurementService unit_MeasurementService)
        {
            _deviceService = deviceService;
            _mapper = mapper;
            _device_StatusService = device_StatusService;
            _system_Type = system_Type;
            _unit_MeasurementService = unit_MeasurementService;
        }


        [HttpPut("CreateSensor")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> CreateSensor(SensorCreatePostDto sensor)
        {
            ServiceResponse response = new ServiceResponse();

            try
            {
                var actuatorNew = await _deviceService.AddSensor(sensor);

                response.Result = ServiceResponseType.Succeded;

                return Ok(response);
            }
            catch (Exception ex)
            {
                response.ErrorMessage = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPut("CreateUnit")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> CreateUnit([FromBody] CreateUnitDto unit)
        {
            ServiceResponse response = new ServiceResponse();

            try
            {
                var unitN = await _unit_MeasurementService.AddUnitMeasurement(unit);

                response.Result = ServiceResponseType.Succeded;

                return Ok(response);
            }
            catch (Exception ex)
            {
                response.ErrorMessage = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPut("CreateActuator")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> CreateActuator(ActuatorCreatePostDto actuator)
        {
            ServiceResponse response = new ServiceResponse();

            try
            {
                var actuatorNew = await _deviceService.AddActuator(actuator);

                response.Result = ServiceResponseType.Succeded;

                return Ok(response);
            }
            catch (Exception ex)
            {
                response.ErrorMessage = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpGet("GetActuators")]
        [Authorize(Roles = "Administrador, Operario")]
        public async Task<ActionResult<IEnumerable<DeviceRequest>>> GetActuators()
        {
            var actuators = await _deviceService.GetActuators();
            return Ok(actuators);
        }

        [HttpGet("GetAllActuators")]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<IEnumerable<DeviceDto>>> GetAllActuators()
        {
            var actuators = await _deviceService.GetAllActuators();
            return Ok(actuators);
        }

        [HttpGet("GetAllSensors")]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<IEnumerable<SensorDto>>> GetAllSensors()
        {
            var sensors = await _deviceService.GetAllSensors();
            return Ok(sensors);
        }

        [HttpGet("GetStates")]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<IEnumerable<Device_StatusDto>>> GetAllStates()
        {
            var states = await _device_StatusService.GetDeviceStates();
            return Ok(states);
        }

        [HttpGet("GetUnits")]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<IEnumerable<Unit_MeasurementDto>>> GetUnits()
        {
            var units = await _unit_MeasurementService.GetUnitMeasurements();
            return Ok(units);
        }

        [HttpGet("GetSystems")]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<IEnumerable<System_TypeDto>>> GetAllSystems()
        {
            var states = await _system_Type.GetSystemTypes();
            return Ok(states);
        }

        [HttpGet("GetSensors")]
        [Authorize(Roles = "Administrador, Operario")]
        public async Task<ActionResult<IEnumerable<DeviceRequestSensors>>> GetSensors()
        {
            var actuators = await _deviceService.GetSensors();
            return Ok(actuators);
        }

        [HttpGet("GetModes")]
        [Authorize(Roles = "Administrador, Operario")]
        public async Task<IActionResult> GetModes()
        {
            var modes = await _deviceService.GetAllAsync();
            return Ok(modes);
        }

        [HttpPut("UpdateMode")]
        [Authorize(Roles = "Administrador, Operario")]
        public async Task<IActionResult> Update(ActuatorModePost actuatorModePost)
        {
            try
            {
                var updated = await _deviceService.UpdateModeAsync(actuatorModePost.Id, actuatorModePost.IsAutoMode);
                return Ok(updated);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// This API method is used to update a user in the database
        /// </summary>
        /// <returns>A response code</returns>
        /// <response code= "200">Customers have been obtained correctly</response>
        /// <response code= "400">The server cannot satisfy a request</response>
        /// <response code= "500">Database connection failure</response>
        [HttpGet("LastMeasurements")]
        [Authorize(Roles = "Administrador, Operario")]
        [ProducesResponseType(typeof(void), 200)]
        [ProducesResponseType(typeof(string), 400)]
        public async Task<ActionResult<IEnumerable<SensorMeasurementDto>>> GetLastMeasurements()
        {
            var result = await _deviceService.GetLastMeasurements();
            return Ok(result);
        }

        /// <summary>
        /// This API method is used to update a user in the database
        /// </summary>
        /// <returns>A response code</returns>
        /// <response code= "200">Customers have been obtained correctly</response>
        /// <response code= "400">The server cannot satisfy a request</response>
        /// <response code= "500">Database connection failure</response>
        [HttpPut]
        [Authorize(Roles = "Administrador, Operario")]
        [ProducesResponseType(typeof(void), 200)]
        [ProducesResponseType(typeof(string), 400)]
        public async Task<IActionResult> UpdateState(Device_PostDto devicePostDto)
        {
            var result = await _deviceService.UpdateStateDevice(devicePostDto.Id, devicePostDto.Device_State);
            return result.Result == ServiceResponseType.Succeded ? Ok() : BadRequest(result.ErrorMessage);
        }

        /// <summary>
        /// This API method is used to update a user in the database
        /// </summary>
        /// <param name="userDto"></param>
        /// <returns>A response code</returns>
        /// <response code= "200">Customers have been obtained correctly</response>
        /// <response code= "400">The server cannot satisfy a request</response>
        /// <response code= "500">Database connection failure</response>
        [HttpPut("ActionActuator")]
        [Authorize(Roles = "Administrador")]
        [ProducesResponseType(typeof(void), 200)]
        [ProducesResponseType(typeof(string), 400)]
        public async Task<IActionResult> TurnOnOffActuator(ActionActuatorDto actionActuatorDto)
        {
            var result = await _deviceService.turnOnOffActuator(actionActuatorDto.Device_Name, actionActuatorDto.State);
            return result.Result == ServiceResponseType.Succeded ? Ok() : BadRequest(result.ErrorMessage);
        }

        /// <summary>
        /// Traer todas las reglas de un sensor (3 impactos: Bajo, Medio, Alto)
        /// </summary>
        [HttpGet("By-device/{deviceId}")]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<List<ControlRuleResponseDto>>> GetByDevice(Guid deviceId)
        {
            var rules = await _deviceService.GetByDeviceAsync(deviceId);
            if (rules == null || !rules.Any())
                return NotFound("No se encontraron reglas para este sensor.");

            return Ok(rules);
        }

        /// <summary>
        /// Actualizar los rangos y actuadores de una regla específica
        /// </summary>
        [HttpPut("Update/{ruleId}")]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<ControlRuleResponseDto>> Update(Guid ruleId, [FromBody] ControlRuleCreateDto request)
        {
            try
            {
                var updatedRule = await _deviceService.UpdateAsync(ruleId, request);
                return Ok(updatedRule);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Actualizar actuador
        /// </summary>
        [HttpPut("UpdateActuador/{actuatorId}")]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<ControlRuleResponseDto>> UpdateActuador(Guid actuatorId, [FromBody] ActuatorPostDto request)
        {
            try
            {
                var updatedRule = await _deviceService.UpdateActuator(actuatorId, request);
                return Ok();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Actualizar actuador
        /// </summary>
        [HttpPut("UpdateSensor/{sensorId}")]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<ControlRuleResponseDto>> UpdateSensor(Guid sensorId, [FromBody] SensorPostDto request)
        {
            try
            {
                var updatedRule = await _deviceService.UpdateSensor(sensorId, request);
                return Ok();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

    }    
}
