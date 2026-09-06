using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Models.Dto;
using RR_Nueva_Naturaleza.Service;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using RR_Nueva_Naturaleza.Models.Request;

namespace RR_Nueva_Naturaleza.ApiControllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ApiChecklistController : ControllerBase
    {
        private readonly IChecklistService _checklistService;

        public ApiChecklistController(IChecklistService checklistService)
        {
            _checklistService = checklistService;
        }

        [Authorize(Roles = "Administrador,Operario")]
        [HttpPost]
        [ProducesResponseType(typeof(void), 200)]
        [ProducesResponseType(typeof(string), 400)]
        public async Task<IActionResult> AddChecklist([FromBody] ChecklistHeaderDto checklistHeaderDto)
        {
            ServiceResponse response = new ServiceResponse();

            try
            {
                if (checklistHeaderDto == null)
                {
                    response.Result = ServiceResponseType.Failed;
                    response.ErrorMessage = "Datos inválidos";
                    return BadRequest(response);
                }

                var result = await _checklistService.AddChecklist(checklistHeaderDto);

                if (result.Result == ServiceResponseType.Failed)
                {
                    return BadRequest(result);
                }

                response.Result = ServiceResponseType.Succeded;
                response.InformationMessage = "Checklist agregado correctamente";
                response.Data = result.Data;

                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Result = ServiceResponseType.Failed;
                response.ErrorMessage = ex.Message;
                return BadRequest(response);
            }
        }

        [Authorize(Roles = "Administrador")]
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(void), 200)]
        [ProducesResponseType(typeof(string), 400)]
        public async Task<IActionResult> GetChecklist(Guid id)
        {
            ServiceResponse response = new ServiceResponse();

            try
            {
                var result = await _checklistService.GetChecklist(id);

                if (result.Result == ServiceResponseType.Failed)
                {
                    return NotFound(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                response.Result = ServiceResponseType.Failed;
                response.ErrorMessage = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpGet("GetAllChecklists")]
        [Authorize(Roles = "Administrador")] 
        public async Task<ActionResult<IEnumerable<ChecklistHeader>>> GetAllChecklists()
        {
            var checklists = await _checklistService.GetAllChecklists();
            return Ok(checklists);
        }

        [HttpGet("GetChecklistsByDate")]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<IEnumerable<ChecklistHeader>>> GetChecklistsByDate(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            var checklists = await _checklistService.GetChecklistsByDate(startDate, endDate);
            return Ok(checklists);
        }

        [HttpGet("GetChecklistsByUser")]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<IEnumerable<ChecklistHeader>>> GetChecklistsByUser([FromQuery] Guid userId)
        {
            var checklists = await _checklistService.GetChecklistsByUser(userId);
            return Ok(checklists);
        }

        [Authorize(Roles = "Administrador,Operario")]
        [HttpGet("GetSensorMeasurements")]
        public async Task<IActionResult> GetSensorMeasurements([FromQuery] Guid deviceId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            try
            {
                var result = await _checklistService.GetSensorMeasurements(deviceId, startDate, endDate);

                return Ok(result); // Se retorna directamente la lista sin ServiceResponse
            }
            catch (Exception ex)
            {
                return BadRequest($"Error obteniendo mediciones: {ex.Message}");
            }
        }


        [HttpPost("DownloadChecklistCsv")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DownloadChecklistCsv([FromBody] ChecklistCsvRequestDto request)
        {
            try
            {
                using var ms = new MemoryStream();
                await _checklistService.GenerateChecklistCsv(ms, request.StartDate, request.EndDate, request.UserId);

                var fileName = $"Checklist_{DateTime.Now:yyyyMMddHHmm}.csv";
                return File(ms.ToArray(), "text/csv", fileName); 
            }
            catch (Exception ex)
            {
                return BadRequest($"Error generando el CSV: {ex.Message}");
            }
        }

        [HttpPost("GeneratePdf")]
        public async Task<IActionResult> GeneratePdf([FromBody] ChecklistPdfRequestDto request)
        {
            using var ms = new MemoryStream();
            await _checklistService.GeneratePdf(ms, request.SensorId, request.StartDate, request.EndDate, request.ChartImage);

            var fileName = $"Reporte_{DateTime.Now:yyyyMMddHHmm}.pdf";

            return File(ms.ToArray(), "application/pdf", fileName);
        }


    }
}