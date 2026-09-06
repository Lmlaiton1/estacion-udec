using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Models.Dto;
using RR_Nueva_Naturaleza.Service;
using System.IO.Ports;
using System.Security.Claims;

namespace RR_Nueva_Naturaleza.ApiControllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ApiSchedulesController : ControllerBase
    {
        private readonly IScheduleService _service;

        public ApiSchedulesController(IScheduleService service)
        {
            _service = service;
        }

        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> GetAll()
        {
            ServiceResponse response = new ServiceResponse();

            try
            {
                return Ok(await _service.GetAllAsync());
            }
            catch (Exception ex)
            {
                response.ErrorMessage = ex.Message;
                return BadRequest(response);
            }
            
        }

        [HttpGet("device/{deviceId}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> GetByDevice(Guid deviceId)
        {
            ServiceResponse response = new ServiceResponse();

            try
            {
                return Ok(await _service.GetByDeviceIdAsync(deviceId));
            }
            catch (Exception ex)
            {
                response.ErrorMessage = ex.Message;
                return BadRequest(response);
            }

        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Create([FromBody] ScheduleDto dto)
        {
            ServiceResponse response = new ServiceResponse();

            try
            {
                var created = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetAll), new { id = created.Id }, created);
            }
            catch (Exception ex)
            {
                response.ErrorMessage = ex.Message;
                return BadRequest(response);
            }
  
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Update(Guid id, [FromBody] ScheduleDto dto)
        {
            var updated = await _service.UpdateAsync(id, dto);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _service.DeleteAsync(id);
            return result ? NoContent() : NotFound();
        }

        [HttpGet("Ports")]
        [AllowAnonymous]
        public IActionResult GetPorts()
        {
            var ports = SerialPort.GetPortNames();
            return Ok(ports);
        }

        [HttpGet("Config")]
        [AllowAnonymous]
        public async Task<IActionResult> GetConfig()
        {
            ServiceResponse response = new ServiceResponse();

            try
            {
                return Ok(await _service.GetConfig());
            }
            catch (Exception ex)
            {
                response.ErrorMessage = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPost("ConfigSerial")]
        [AllowAnonymous]
        public async Task<IActionResult> SaveConfig([FromBody] SerialPortConfigDto dto)
        {
            var updated = await _service.UpdateConfig(dto);
            if (updated == null) return NotFound();
            return Ok(updated);
        }


    }
}
