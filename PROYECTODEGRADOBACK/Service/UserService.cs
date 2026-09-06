using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RR_Nueva_Naturaleza.DAL;
using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Models.Common;
using RR_Nueva_Naturaleza.Models.Dto;
using RR_Nueva_Naturaleza.Models.Request;
using RR_Nueva_Naturaleza.Models.Response;
using RR_Nueva_Naturaleza.Tools;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RR_Nueva_Naturaleza.Service
{
    public class UserService : IUserService
    {
        private readonly RR_Nueva_NaturalezaContext _context;
        private readonly IMapper _mapper;
        private readonly AppSettings _appSettings;

        public UserService(RR_Nueva_NaturalezaContext context, IOptions<AppSettings> appSettings, IMapper mapper) //Constructor con inyeccion de dependencias
        {
            _context = context;
            _mapper = mapper;
            _appSettings = appSettings.Value;
        }

        public async Task<ServiceResponse> AddUser(string name, string lastName, string idCard, int question, string answer, Guid rol)
        {
            try
            {
                string spassword = Encrypt.HashBCrypt(idCard);

                await _context.Users.AddAsync(new User()
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Last_Name = lastName,
                    Id_Card = idCard,
                    Password = spassword,
                    Questions_Type = question,
                    Answer = answer,
                    RolId = rol
                });
                await _context.SaveChangesAsync();

                return new ServiceResponse()
                {
                    Result = ServiceResponseType.Succeded,
                    InformationMessage = "User add Correct"
                };
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<User?> GetUser(Guid UserId)
        {
            return await _context.Users.FindAsync(UserId);
        }

        public async Task<IEnumerable<User>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<ServiceResponse> UpdateUser(Guid UserId, string name, string lastName, string idCard, string password, string rol)
        {
            try
            {
                var rolId = new Rol();
                var admin = await _context.Rols.FirstOrDefaultAsync();

                if (rol == "Administrador")
                {
                    rolId = admin;
                }
                else
                {
                    rolId = await _context.Rols.Skip(1).FirstOrDefaultAsync();
                }

                var user = await _context.Users.FindAsync(UserId);
                if (user == null)
                {
                    return new ServiceResponse()
                    {
                        Result = ServiceResponseType.Failed,
                        ErrorMessage = "user don't exist"
                    };
                }

                user.Name = name;
                user.Last_Name = lastName;
                user.Id_Card = idCard;
                if (!string.IsNullOrEmpty(password))
                {
                    user.Password = Encrypt.HashBCrypt(password);
                }
                user.RolId = rolId.Id;

                _context.Users.Update(user);

                await _context.SaveChangesAsync();
                return new ServiceResponse()
                {
                    Result = ServiceResponseType.Succeded
                };

            }
            catch (Exception ex)
            {
                return new ServiceResponse()
                {
                    Result = ServiceResponseType.Failed,
                    ErrorMessage = ex.Message

                };
            }
        }

        public async Task<ServiceResponse> DeleteUser(Guid userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return new ServiceResponse()
                    {
                        Result = ServiceResponseType.Failed,
                        ErrorMessage = "user don't exist"
                    };
                }
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                return new ServiceResponse()
                {
                    Result = ServiceResponseType.Succeded

                };

            }
            catch (Exception ex)
            {
                return new ServiceResponse()
                {
                    Result = ServiceResponseType.Failed,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<UserResponse> Auth(AuthRequest model)
        {

            UserResponse userresponse = new UserResponse();

            var user = await _context.Users.Where(c => c.Id_Card == model.user).Include(x => x.Rol).FirstOrDefaultAsync();

            if (user == null)
            {
                // Ejecuta una verificación BCrypt contra un hash dummy para que esta rama
                // tarde lo mismo que la de credenciales inválidas de más abajo, y no se
                // puedan enumerar cédulas válidas midiendo el tiempo de respuesta.
                Encrypt.VerifyDummyBCrypt(model.Password);
                return null;
            }

            bool passwordOk;

            if (Encrypt.IsLegacySha256Hash(user.Password))
            {
                passwordOk = user.Password == Encrypt.GetSHA256(model.Password);
                if (passwordOk)
                {
                    // Migración transparente: solo se re-hashea si la contraseña legada
                    // en SHA-256 coincidió; si falló, no se toca la base de datos.
                    user.Password = Encrypt.HashBCrypt(model.Password);
                    _context.Users.Update(user);
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                passwordOk = Encrypt.VerifyBCrypt(model.Password, user.Password);
            }

            if (!passwordOk)
            {
                return null;
            }

            var userRequest = _mapper.Map<User, UserRequest>(user);

            userresponse.UserId = user.Id;
            userresponse.UserName = user.Name;
            userresponse.UserLastName = user.Last_Name;
            userresponse.Token = GetToken(userRequest);


            return userresponse;

        }

        public async Task<ServiceResponse> ForgetPassword(Forget_PasswordDto forget)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(x => x.Id_Card == forget.user && x.Questions_Type == forget.question_type && x.Answer == forget.answer);
                if (user == null)
                {
                    return new ServiceResponse()
                    {
                        Result = ServiceResponseType.Failed,
                    };
                }
                user.Password = Encrypt.HashBCrypt(forget.newPassword);
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                return new ServiceResponse()
                {
                    Result = ServiceResponseType.Succeded,
                    InformationMessage = "La contraseña ha sido cambiada correctamente"
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse()
                {
                    Result = ServiceResponseType.Failed,
                    ErrorMessage = ex.Message
                };
            }
        }

        private string GetToken(UserRequest user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var key = Encoding.ASCII.GetBytes(_appSettings.secret);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new System.Security.Claims.ClaimsIdentity(

                    new Claim[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim("name", user.Name),
                        new Claim("rolId", user.RolId.ToString()),
                        new Claim(ClaimTypes.Role, user.RolName.ToString()),

                    }

                    ),

                Expires = DateTime.UtcNow.AddHours(8),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

    }
}
