using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Models.Dto;
using RR_Nueva_Naturaleza.Models.Request;
using RR_Nueva_Naturaleza.Service;
using System.Security.Claims;

namespace RR_Nueva_Naturaleza.ApiControllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ApiEventController : ControllerBase
    {

        private readonly IEventService _eventService;
        private readonly IMapper _mapper;
        public ApiEventController(IEventService eventService, IMapper mapper)
        {
            _eventService = eventService;
            _mapper = mapper;
        }

        [Authorize(Roles = "Administrador, Operario")]
        [HttpPost]
        [ProducesResponseType(typeof(void), 200)]
        [ProducesResponseType(typeof(string), 400)]
        public async Task<IActionResult> AddEvent(EventCreateDto eventCreateDto)
        {
            ServiceResponse response = new ServiceResponse();

            try
            {
                var eventAdd = await _eventService.AddEventActuator(eventCreateDto.DeviceId, eventCreateDto.DeviceName, eventCreateDto.State);

                response.Result = ServiceResponseType.Succeded;

                return Ok(response);
            }
            catch (Exception ex)
            {
                response.ErrorMessage = ex.Message;
                return BadRequest(response);
            }

        }
        [Authorize(Roles = "Administrador, Operario")]
        [HttpGet("GetEventsFalse")]
        public async Task<ActionResult<IEnumerable<EventNotifyDto>>> GetEventsFalse()
        {
            var eventsFalse = await _eventService.GetEventFalse();
            return Ok(eventsFalse);
        }

        [AllowAnonymous]
        [HttpPost("Notify")]
        public async Task<IActionResult> NotifyEvent([FromBody] Guid eventId)
        {
            ServiceResponse response = new ServiceResponse();

            try
            {
                await _eventService.NotifyAsync(eventId);

                response.Result = ServiceResponseType.Succeded;

                return Ok(response);
            }
            catch (Exception ex)
            {
                response.ErrorMessage = ex.Message;
                return BadRequest(response);
            }
        }

        [Authorize(Roles = "Administrador, Operario")]
        [HttpPut("MarkAsSeen/{id}")]
        public async Task<IActionResult> MarkAsSeen(Guid id)
        {
            ServiceResponse response = new ServiceResponse();

            try
            {
                var eventAdd = await _eventService.MarkAsSeen(id);

                response.Result = ServiceResponseType.Succeded;

                return Ok(response);
            }
            catch (Exception ex)
            {
                response.ErrorMessage = ex.Message;
                return BadRequest(response);
            }
        }

        [Authorize(Roles = "Administrador, Operario")]
        [HttpPut("MarkAllAsSeen")]
        public async Task<IActionResult> MarkAllAsSeen()
        {
            ServiceResponse response = new ServiceResponse();

            try
            {
                var eventAdd = await _eventService.MarkAllAsSeen();

                response.Result = ServiceResponseType.Succeded;

                return Ok(response);
            }
            catch (Exception ex)
            {
                response.ErrorMessage = ex.Message;
                return BadRequest(response);
            }
        }

    }
}
