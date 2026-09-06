using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RR_Nueva_Naturaleza.Models.Dto;
using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Service;
using System.Security.Claims;

namespace RR_Nueva_Naturaleza.ApiControllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApiAuditController : ControllerBase
    {
        private readonly IAuditService _auditService;
        private readonly IMapper _mapper;
        public ApiAuditController(IAuditService auditService, IMapper mapper)
        {
            _auditService = auditService;
            _mapper = mapper;
        }

        [HttpPost]
        [ProducesResponseType(typeof(void), 200)]
        [ProducesResponseType(typeof(string), 400)]
        public async Task<IActionResult> AddAudit(AuditCreateDto auditCreateDto)
        {
            ServiceResponse response = new ServiceResponse();

            try
            {
                var user = User.FindFirst(ClaimTypes.NameIdentifier).Value;

                var audit = await _auditService.AddAudit(auditCreateDto.Action, auditCreateDto.Observation, Guid.Parse(user), auditCreateDto.DeviceId);

                response.Result = ServiceResponseType.Succeded;

                return Ok(response);
            }
            catch (Exception ex)
            {
                response.ErrorMessage = ex.Message;
                return BadRequest(response);
            }

        }

        [HttpGet("AuditByUserDate")]
        public async Task<IActionResult> GetAuditByUserDate(Guid userId, DateTime startDate, DateTime endDate)
        {
            var users = await _auditService.GetAuditByUserDate(userId, startDate, endDate);
            return Ok(users);
        }

        [HttpPost("AuditByUserDateForPdf")]
        public async Task<IActionResult> GetAuditByUserDateForPdf([FromBody] AuditUserPdfRequestDto request)
        {
            // Generar el PDF en memoria
            using var ms = new MemoryStream();
            await _auditService.GetAuditByUserDateForPdf(ms, request.UserId, request.StartDate,
                request.EndDate, request.ChartImage);

            var fileName = $"Reporte_{DateTime.Now:yyyyMMddHHmm}.pdf";
            return File(ms.ToArray(), "application/pdf", fileName);
        }
    }
}
