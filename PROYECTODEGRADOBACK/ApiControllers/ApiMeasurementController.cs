using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Models.Dto;
using RR_Nueva_Naturaleza.Service;

namespace RR_Nueva_Naturaleza.ApiControllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApiMeasurementController : ControllerBase
    {
        private readonly IMeasurementService _measurementService;

        public ApiMeasurementController(IMeasurementService measurementService)
        {
            _measurementService = measurementService;
        }

        [HttpGet("MeasurementByDate")]
        public async Task<IActionResult> GetMeasurementsByDate(Guid measurementTypeId, DateTime startDate, DateTime endDate)
        {
            var measurements = await _measurementService.GetMeasurementsByDate(measurementTypeId, startDate, endDate);
            return Ok(measurements);
        }

        [HttpPost("MeasurementForPdf")]
        public async Task<IActionResult> GetMeasurementsForPdf([FromBody] MeasurementPdfRequestDto request)
        {
            // Generar el PDF en memoria
            using var ms = new MemoryStream();
            await _measurementService.GetMeasurementsForPdf(ms, request.MeasurementTypeId, request.StartDate,
                request.EndDate, request.ChartImage);

            var fileName = $"Reporte_{DateTime.Now:yyyyMMddHHmm}.pdf";
            return File(ms.ToArray(), "application/pdf", fileName);
        }

        [HttpPost("GenerateAllMeasurementsCsv")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> GenerateAllMeasurementsCsv([FromBody] ChecklistCsvRequestDto request)
        {
            try
            {
                using var ms = new MemoryStream();
                await _measurementService.GenerateAllMeasurementsCsv(ms, request.StartDate, request.EndDate, request.UserId);

                var fileName = $"Mediciones_{request.StartDate:yyyyMMdd}_{request.EndDate:yyyyMMdd}.csv";
                return File(ms.ToArray(), "text/csv", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error generando el CSV: {ex.Message}");
            }
        }

    }
}
