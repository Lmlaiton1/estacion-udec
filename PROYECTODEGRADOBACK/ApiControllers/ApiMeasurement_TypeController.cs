using Microsoft.AspNetCore.Mvc;
using RR_Nueva_Naturaleza.Service;
using RR_Nueva_Naturaleza.Models;

namespace RR_Nueva_Naturaleza.ApiControllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApiMeasurement_TypeController : ControllerBase
    {
        private readonly IMeasurement_TypeService _measurementTypeService;

        public ApiMeasurement_TypeController(IMeasurement_TypeService measurementTypeService)
        {
            _measurementTypeService = measurementTypeService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllMeasurementTypes()
        {
            var types = await _measurementTypeService.GetMeasurementTypes();
            return Ok(types);
        }
    }
}