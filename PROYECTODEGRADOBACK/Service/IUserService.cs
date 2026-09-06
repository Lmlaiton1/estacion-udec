using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Models.Request;
using RR_Nueva_Naturaleza.Models.Response;
using RR_Nueva_Naturaleza.Models.Request;
using RR_Nueva_Naturaleza.Models.Response;
using RR_Nueva_Naturaleza.Models.Dto;

namespace RR_Nueva_Naturaleza.Service
{
    public interface IUserService
    {
        Task<ServiceResponse> AddUser(string name, string last_Name, string id_Card, int question, string answer, Guid rol);
        Task<User?> GetUser(Guid UserId);
        Task<IEnumerable<User>> GetUsers(); // El metodo returna una coleecion de objetos (Lista)
        Task<ServiceResponse> UpdateUser(Guid UserId, string name, string last_Name, string id_Card, string password, string rol);
        Task<ServiceResponse> DeleteUser(Guid UserId);
        Task<UserResponse> Auth(AuthRequest model);
        Task<ServiceResponse> ForgetPassword(Forget_PasswordDto forget);
    }
}
