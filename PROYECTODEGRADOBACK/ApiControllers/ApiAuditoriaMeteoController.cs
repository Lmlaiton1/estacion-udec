using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Models.Dto;
using RR_Nueva_Naturaleza.Service;

namespace RR_Nueva_Naturaleza.ApiControllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Administrador")]
    public class ApiAuditoriaMeteoController : ControllerBase
    {
        private readonly IAuditoriaMeteoService _auditoriaMeteoService;
        private readonly IMapper _mapper;

        public ApiAuditoriaMeteoController(IAuditoriaMeteoService auditoriaMeteoService, IMapper mapper)
        {
            _auditoriaMeteoService = auditoriaMeteoService;
            _mapper = mapper;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<AuditoriaMeteoDto>), 200)]
        public async Task<IEnumerable<AuditoriaMeteoDto>> GetAuditoria()
        {
            var auditoria = await _auditoriaMeteoService.GetAuditoria();
            return _mapper.Map<List<AuditoriaMeteo>, List<AuditoriaMeteoDto>>(auditoria.ToList());
        }
    }
}
