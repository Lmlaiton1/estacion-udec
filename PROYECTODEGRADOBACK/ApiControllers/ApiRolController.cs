using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RR_Nueva_Naturaleza.Models.Dto;
using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Service;
using Microsoft.AspNetCore.Authorization;

namespace RR_Nueva_Naturaleza.ApiControllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ApiRolController : ControllerBase
    {

        private readonly IRolService _rolService;
        private readonly IMapper _mapper; 

        public ApiRolController(IRolService rolService, IMapper mapper)
        {
            _rolService = rolService;
            _mapper = mapper;
        }
        /// <summary>
        /// This API method is where we get all the rols registered in our database.
        /// </summary>
        /// <returns>A list of rols.</returns>
        /// <response code= "200">Customers have been obtained correctly</response>
        /// <response code= "400">The server cannot satisfy a request</response>
        /// <response code= "500">Database connection failure</response>
        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<RolDto>), 200)]
        public async Task<IEnumerable<RolDto>> GetRols()
        {
            var rols = await _rolService.GetRols();
            var roleList = _mapper.Map<List<Rol>, List<RolDto>>(rols.ToList());
            return roleList;
        }

        /// <summary>
        /// This API method is used to get the rol to the database
        /// </summary>
        /// <param name="id"></param>
        /// <returns>A response code</returns>
        /// <response code= "200">Customers have been obtained correctly</response>
        /// <response code= "400">The server cannot satisfy a request</response>
        /// <response code= "500">Database connection failure</response>
        [Authorize(Roles = "Administrador,Operario")]
        [HttpGet("obtener/{id}")]
        [ProducesResponseType(typeof(RolDto), 200)]
        [ProducesResponseType(typeof(string), 400)]
        public async Task<RolDto> GetRol(Guid id)
        {
            var roleOne = await _rolService.GetRol(id);
            var roleOneId = _mapper.Map<Rol, RolDto>(roleOne);
            return roleOneId;
        }

    }
}
