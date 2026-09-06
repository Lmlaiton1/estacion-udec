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
    public class ApiUserController : ControllerBase
    {

        private readonly IUserService _userService;
        private readonly IMapper _mapper;

        public ApiUserController(IUserService userService, IMapper mapper)
        {
            _userService = userService;
            _mapper = mapper;
        }
        [Authorize(Roles = "Administrador")]
        [HttpPost]
        [ProducesResponseType(typeof(void), 200)]
        [ProducesResponseType(typeof(string), 400)]
        public async Task<IActionResult> AddUser(UserCreateDto userCreateDto)
        {
            ServiceResponse response = new ServiceResponse();

            try
            {
                var id = Guid.NewGuid();

                var user = await _userService.AddUser(userCreateDto.Name, userCreateDto.Last_Name, userCreateDto.Id_Card, userCreateDto.Question_Type, userCreateDto.Answer, userCreateDto.RolId);

                
                response.Result = ServiceResponseType.Succeded;

                return Ok(response);
            }
            catch (Exception ex)
            {
                response.ErrorMessage = ex.Message;
                return BadRequest(response);
            }
        }

        /// <summary>
        /// his API method is where we get all the users registered in our database.
        /// </summary>
        /// <returns>A list of users</returns>
        /// <response code= "200">Customers have been obtained correctly</response>
        /// <response code= "400">The server cannot satisfy a request</response>
        /// <response code= "500">Database connection failure</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<UserDto>), 200)]
        public async Task<IEnumerable<UserDto>> GetUsers()
        {
            var Users = await _userService.GetUsers();
            var UsersList = _mapper.Map<List<User>, List<UserDto>>(Users.ToList());
            return UsersList;
        }

        [AllowAnonymous]
        [HttpPost("Login")]
        [ProducesResponseType(typeof(void), 200)]
        [ProducesResponseType(typeof(string), 400)]
        public async Task<IActionResult> Autentification([FromBody] AuthRequest user)
        {

            ServiceResponse response = new ServiceResponse();
            try
            {
              
                var userresponse = await _userService.Auth(user);

                if (userresponse == null)
                {
                    response.Result = ServiceResponseType.Failed;
                    response.ErrorMessage = "Usuario o contraseña incorrecta";
                    return BadRequest(response);
                }

                response.Result = ServiceResponseType.Succeded;
                response.Data = userresponse;

                return Ok(response);
            }
            catch(Exception ex)
            {
                response.ErrorMessage = ex.Message;
                return BadRequest(response);
            }
           
        }

        /// <summary>
        /// This API method is used to update a user in the database
        /// </summary>
        /// <param name="userDto"></param>
        /// <returns>A response code</returns>
        /// <response code= "200">Customers have been obtained correctly</response>
        /// <response code= "400">The server cannot satisfy a request</response>
        /// <response code= "500">Database connection failure</response>
        [HttpPut]
        [ProducesResponseType(typeof(void), 200)]
        [ProducesResponseType(typeof(string), 400)]
        public async Task<IActionResult> UpdateUser(User_PostDto userPostDto)
        {
            var result = await _userService.UpdateUser(userPostDto.Id, userPostDto.Name, userPostDto.Last_Name, userPostDto.Id_Card, userPostDto.Password, userPostDto.Rol);
            return result.Result == ServiceResponseType.Succeded ? Ok() : BadRequest(result.ErrorMessage);
        }

        /// <summary>
        /// This API method is used to delete a reservation in the database
        /// </summary>
        /// <param name="userDto"></param>
        /// <returns>A response code</returns>
        /// <response code= "200">Customers have been obtained correctly</response>
        /// <response code= "400">The server cannot satisfy a request</response>
        /// <response code= "500">Database connection failure</response>
        [HttpDelete]
        [ProducesResponseType(typeof(void), 200)]
        [ProducesResponseType(typeof(string), 400)]
        public async Task<IActionResult> DeleteUser(UserDto userDto)
        {
            var result = await _userService.DeleteUser(userDto.Id);
            return result.Result == ServiceResponseType.Succeded ? Ok() : BadRequest(result.ErrorMessage);
        }

        [AllowAnonymous]
        [HttpPost("ForgetPassword")]
        [ProducesResponseType(typeof(void), 200)]
        [ProducesResponseType(typeof(string), 400)]
        public async Task<IActionResult> ForgetPassword([FromBody] Forget_PasswordDto forget)
        {

            ServiceResponse response = new ServiceResponse();
            try
            {

                var userresponse = await _userService.ForgetPassword(forget);

                if (userresponse.Result == ServiceResponseType.Failed)
                {
                    response.Result = ServiceResponseType.Failed;
                    response.ErrorMessage = "El usuario no existe o los datos son incorrectos";
                    return BadRequest(response);
                }

                response.Result = ServiceResponseType.Succeded;
                response.InformationMessage = "Contraseña actualizada correctamente";
                response.Data = userresponse;

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
