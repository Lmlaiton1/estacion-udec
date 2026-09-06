using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using RR_Nueva_Naturaleza.DAL;
using RR_Nueva_Naturaleza.Hubs;
using RR_Nueva_Naturaleza.Models.Common;
using RR_Nueva_Naturaleza.Service;
using RR_Nueva_Naturaleza.Utilities;
using System.Reflection;
using System.Text;

namespace RR_Nueva_Naturaleza
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string rrnn = "RRNN";
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Configuración de base de datos
            builder.Services.AddDbContext<RR_Nueva_NaturalezaContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Configuración de AppSettings
            var appSettingsSection = builder.Configuration.GetSection("AppSettings");
            builder.Services.Configure<AppSettings>(appSettingsSection);

            var appSettings = appSettingsSection.Get<AppSettings>();
            var key = Encoding.ASCII.GetBytes(appSettings.secret); // Nota: nombre en mayúscula para seguir convención

            // Configuración de JWT Authentication
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false, // Cambiar a true si se define un issuer
                    ValidateAudience = false, // Cambiar a true si se define un audience
                    ClockSkew = TimeSpan.Zero // Para evitar desfases de tiempo en tokens
                };
            });

            // Inyección de dependencias
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IRolService, RolService>();
            builder.Services.AddScoped<IDeviceService, DeviceService>();
            builder.Services.AddScoped<IDevice_TypeService, Device_TypeService>();
            builder.Services.AddScoped<IDevice_StatusService, Device_StatusService>();
            builder.Services.AddScoped<ISystem_Type, System_TypeService>();
            builder.Services.AddScoped<IEventService, EventService>();
            builder.Services.AddScoped<IAuditService, AuditService>();
            builder.Services.AddScoped<IMeasurementService, MeasurementService>();
            builder.Services.AddScoped<IImpactService, ImpactService>();
            builder.Services.AddScoped<IMeasurement_TypeService, Measurement_TypeService>();
            builder.Services.AddScoped<IUnit_MeasurementService, Unit_MeasurementService>();
            builder.Services.AddScoped<IScheduleService, ScheduleService>();
            builder.Services.AddScoped<IChecklistService, ChecklistService>();


            // AutoMapper
            builder.Services.AddAutoMapper(config => config.AddMaps(Assembly.GetExecutingAssembly()));

            // CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy(name: rrnn, builder =>
                {
                    builder.SetIsOriginAllowed(_ => true)
                           .AllowAnyHeader()
                           .AllowAnyMethod()
                           .AllowCredentials();
                });
            });

            // SignalR
            builder.Services.AddSignalR();

            var app = builder.Build();

            // Swagger
            app.UseSwagger();
            app.UseSwaggerUI();

            // Routing y CORS
            app.UseRouting();
            app.UseCors(rrnn);

            // Importante: Orden correcto de middlewares
            app.UseAuthentication(); // Debe ir antes de Authorization
            app.UseAuthorization();

            app.MapControllers();
            app.MapHub<NotificationHub>("/Hubs/Notification").RequireCors(rrnn);
            app.MapHub<ActuatorHub>("/Hubs/Actuators").RequireCors(rrnn);
            app.MapHub<StateHub>("/Hubs/State").RequireCors(rrnn);
            app.MapHub<SdTransferHub>("/Hubs/SdTransfer").RequireCors(rrnn);

            app.Run();
        }
    }
}
