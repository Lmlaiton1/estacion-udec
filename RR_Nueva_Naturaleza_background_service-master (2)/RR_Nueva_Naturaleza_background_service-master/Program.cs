using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ServicioSensorica.Data;
using ServicioSensorica.Worker;

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        options.ServiceName = "Servicio Sensorica"; // Nombre del servicio
    })
    .ConfigureServices((context, services) =>
    {
        var connectionString = context.Configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Registramos el Worker
        services.AddHostedService<Worker>();
    })
    .ConfigureLogging((context, logging) =>
    {
        logging.ClearProviders();

        // Log en consola solo si se ejecuta manualmente
        if (Environment.UserInteractive)
        {
            logging.AddConsole();
        }

        // Log al visor de eventos cuando es servicio Windows
        logging.AddEventLog(settings =>
        {
            settings.SourceName = "ServicioSensorica";
        });
    })
    .Build();

await host.RunAsync();
