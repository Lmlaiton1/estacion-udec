using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using ServicioSensorica.Data;
using ServicioSensorica.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ServicioSensorica.Worker
{
    // Suscriptor MQTT de las 3 estaciones meteorológicas. No modifica Worker.cs ni AppDbContext.
    public class WeatherMqttWorker : BackgroundService
    {
        private readonly ILogger<WeatherMqttWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;

        private IManagedMqttClient? _mqttClient;
        private volatile HubConnection? _hubConnection;
        private int _reconectandoHub;

        private readonly string _mqttBroker;
        private readonly int _mqttPort;
        private readonly string _weatherHubUrl;

        // GUIDs de sensores, iguales a los que envía el firmware
        private static readonly Guid SensorTemperaturaHumedad = Guid.Parse("aa07acd0-57e0-4ae0-bf46-f8d7c94b4040");
        private static readonly Guid SensorRadiacion = Guid.Parse("6319081a-9f85-4e87-8759-6895d08d935a");
        private static readonly Guid SensorVelocidadViento = Guid.Parse("7c2f1e94-5a3d-4b18-9e6c-2d8f4a1b7e30");
        private static readonly Guid SensorDireccion = Guid.Parse("3e8b6d52-9c17-4a2f-b5d3-6f1e9a4c8b72");
        private static readonly Guid SensorLluvia = Guid.Parse("b91d4f27-6e83-45ca-8d19-4a7c2f5e3b60");

        public WeatherMqttWorker(ILogger<WeatherMqttWorker> logger, IServiceScopeFactory scopeFactory, IConfiguration configuration)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _configuration = configuration;

            _mqttBroker = _configuration["Mqtt:Broker"] ?? "localhost";
            _mqttPort = int.TryParse(_configuration["Mqtt:Port"], out var port) ? port : 1883;
            _weatherHubUrl = _configuration["WeatherHub:Url"] ?? "http://localhost:5005/Hubs/Weather";
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // La conexión al WeatherHub corre en background: no debe bloquear el arranque de MQTT.
            IniciarConexionHubSiNecesario(stoppingToken);

            _mqttClient = await CrearClienteMqttAsync(stoppingToken);

            _logger.LogInformation("WeatherMqttWorker iniciado. Escuchando estacion1/datos, estacion2/datos, estacion3/datos en {broker}:{port}", _mqttBroker, _mqttPort);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (_hubConnection == null || _hubConnection.State == HubConnectionState.Disconnected)
                    {
                        if (IniciarConexionHubSiNecesario(stoppingToken))
                        {
                            _logger.LogWarning("WeatherHub desconectado, intentando reconectar en background...");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error verificando conexión con WeatherHub");
                }

                await Task.Delay(5000, stoppingToken);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_mqttClient != null)
            {
                await _mqttClient.StopAsync();
                _mqttClient.Dispose();
            }

            if (_hubConnection != null)
            {
                await _hubConnection.DisposeAsync();
            }

            await base.StopAsync(cancellationToken);
        }

        private async Task<IManagedMqttClient> CrearClienteMqttAsync(CancellationToken stoppingToken)
        {
            var mqttFactory = new MqttFactory();
            var client = mqttFactory.CreateManagedMqttClient();

            client.ApplicationMessageReceivedAsync += async e =>
            {
                try
                {
                    await ProcesarMensajeAsync(e.ApplicationMessage.Topic, Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error procesando mensaje MQTT del topic {topic}", e.ApplicationMessage.Topic);
                }
            };

            client.ConnectedAsync += _ =>
            {
                _logger.LogInformation("Conectado al broker MQTT {broker}:{port}", _mqttBroker, _mqttPort);
                return Task.CompletedTask;
            };

            client.DisconnectedAsync += _ =>
            {
                _logger.LogWarning("Desconectado del broker MQTT, MQTTnet reintentará automáticamente...");
                return Task.CompletedTask;
            };

            var clientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(_mqttBroker, _mqttPort)
                .WithClientId($"ServicioSensorica-Weather-{Environment.MachineName}")
                .Build();

            var managedOptions = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(clientOptions)
                .Build();

            await client.SubscribeAsync(new[]
            {
                new MqttTopicFilterBuilder().WithTopic("estacion1/datos").Build(),
                new MqttTopicFilterBuilder().WithTopic("estacion2/datos").Build(),
                new MqttTopicFilterBuilder().WithTopic("estacion3/datos").Build(),
            });

            await client.StartAsync(managedOptions);

            return client;
        }

        // Evita lanzar varios intentos de conexión al hub en paralelo (uno inicial + uno del watchdog).
        private bool IniciarConexionHubSiNecesario(CancellationToken stoppingToken)
        {
            if (Interlocked.CompareExchange(ref _reconectandoHub, 1, 0) != 0)
                return false;

            _ = ConectarWeatherHubEnBackgroundAsync(stoppingToken);
            return true;
        }

        // Corre en background: nunca debe bloquear el arranque ni el guardado en BD del lado MQTT.
        private async Task ConectarWeatherHubEnBackgroundAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var connection = new HubConnectionBuilder()
                            .WithUrl(_weatherHubUrl)
                            .WithAutomaticReconnect()
                            .Build();

                        await connection.StartAsync(stoppingToken);
                        _hubConnection = connection;
                        _logger.LogInformation("Conectado al WeatherHub en {url}", _weatherHubUrl);
                        return;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error conectando con WeatherHub. Reintentando en 5 segundos...");
                        await Task.Delay(5000, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Cancelado durante el shutdown del worker, nada que hacer.
            }
            finally
            {
                Interlocked.Exchange(ref _reconectandoHub, 0);
            }
        }

        private async Task ProcesarMensajeAsync(string topic, string payload, CancellationToken stoppingToken)
        {
            var fechaRecepcion = DateTime.Now;
            _logger.LogInformation("MQTT recibido en {topic}: {payload}", topic, payload);

            EstacionDatosDto? datos;
            try
            {
                datos = JsonSerializer.Deserialize<EstacionDatosDto>(payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deserializando payload de {topic}: {payload}", topic, payload);
                return;
            }

            if (datos == null)
            {
                _logger.LogWarning("Payload vacío o inválido en {topic}", topic);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<WeatherDbContext>();

            var crudo = new LecturaCruda
            {
                Id = Guid.NewGuid(),
                EstacionNumero = datos.Estacion,
                Topic = topic,
                Payload = payload,
                FechaRecepcion = fechaRecepcion,
                Procesado = false
            };

            db.LecturasCrudas.Add(crudo);

            try
            {
                await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error guardando lectura cruda de la estación {estacion}", datos.Estacion);
                return;
            }

            if (!DateTime.TryParse(datos.Timestamp, out var fechaHora))
            {
                _logger.LogWarning("Timestamp inválido en {topic}: '{t}', se usa la fecha de recepción", topic, datos.Timestamp);
                fechaHora = fechaRecepcion;
            }

            var lecturas = ConstruirLecturas(datos, fechaHora, fechaRecepcion);

            try
            {
                db.LecturasMeteo.AddRange(lecturas);
                crudo.Procesado = true;
                await db.SaveChangesAsync(stoppingToken);

                _logger.LogInformation("Guardadas {count} lecturas de la estación {estacion}", lecturas.Count, datos.Estacion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error guardando lecturas meteo de la estación {estacion}", datos.Estacion);
                return;
            }

            await NotificarHubAsync(datos, fechaHora);
        }

        private static List<LecturaMeteo> ConstruirLecturas(EstacionDatosDto datos, DateTime fechaHora, DateTime fechaRecepcion)
        {
            string? direccionCardinal = GradosACardinal(datos.DireccionGrados);

            return new List<LecturaMeteo>
            {
                new LecturaMeteo
                {
                    Id = Guid.NewGuid(),
                    EstacionNumero = datos.Estacion,
                    SensorId = SensorTemperaturaHumedad,
                    Variable = "temperatura",
                    Valor = datos.Temperatura,
                    Unidad = "C",
                    FechaHora = fechaHora,
                    FechaRecepcion = fechaRecepcion
                },
                new LecturaMeteo
                {
                    Id = Guid.NewGuid(),
                    EstacionNumero = datos.Estacion,
                    SensorId = SensorTemperaturaHumedad,
                    Variable = "humedad",
                    Valor = datos.Humedad,
                    Unidad = "%",
                    FechaHora = fechaHora,
                    FechaRecepcion = fechaRecepcion
                },
                new LecturaMeteo
                {
                    Id = Guid.NewGuid(),
                    EstacionNumero = datos.Estacion,
                    SensorId = SensorRadiacion,
                    Variable = "radiacion",
                    Valor = datos.Radiacion,
                    Unidad = "Lux",
                    FechaHora = fechaHora,
                    FechaRecepcion = fechaRecepcion
                },
                new LecturaMeteo
                {
                    Id = Guid.NewGuid(),
                    EstacionNumero = datos.Estacion,
                    SensorId = SensorVelocidadViento,
                    Variable = "velocidadViento",
                    Valor = datos.VelocidadViento,
                    Unidad = "m/s",
                    FechaHora = fechaHora,
                    FechaRecepcion = fechaRecepcion,
                    PulsosRaw = datos.VelocidadVientoPulsos,
                    IntervaloMs = datos.VelocidadVientoIntervaloMs
                },
                new LecturaMeteo
                {
                    Id = Guid.NewGuid(),
                    EstacionNumero = datos.Estacion,
                    SensorId = SensorDireccion,
                    Variable = "direccion",
                    Valor = datos.DireccionGrados,
                    ValorCardinal = direccionCardinal,
                    Unidad = "grados",
                    FechaHora = fechaHora,
                    FechaRecepcion = fechaRecepcion
                },
                new LecturaMeteo
                {
                    Id = Guid.NewGuid(),
                    EstacionNumero = datos.Estacion,
                    SensorId = SensorLluvia,
                    Variable = "lluvia",
                    Valor = datos.Lluvia,
                    Unidad = "mm/min",
                    FechaHora = fechaHora,
                    FechaRecepcion = fechaRecepcion,
                    PulsosRaw = datos.LluviaPulsos
                }
            };
        }

        // Convierte grados (0-360) al punto cardinal más cercano en 8 sectores. -1 = sin dato.
        private static string? GradosACardinal(int grados)
        {
            if (grados < 0) return null;

            string[] direcciones = { "N", "NE", "E", "SE", "S", "SO", "O", "NO" };
            int indice = (int)Math.Round((grados % 360) / 45.0) % 8;
            return direcciones[indice];
        }

        private async Task NotificarHubAsync(EstacionDatosDto datos, DateTime fechaHora)
        {
            if (_hubConnection == null || _hubConnection.State != HubConnectionState.Connected)
                return;

            try
            {
                await _hubConnection.InvokeAsync("NotificarLecturaMeteo", new
                {
                    estacion = datos.Estacion,
                    fechaHora,
                    temperatura = datos.Temperatura,
                    humedad = datos.Humedad,
                    radiacion = datos.Radiacion,
                    velocidadViento = datos.VelocidadViento,
                    direccionGrados = datos.DireccionGrados,
                    direccionCardinal = GradosACardinal(datos.DireccionGrados),
                    lluvia = datos.Lluvia
                });

                _logger.LogInformation("Notificado por SignalR al WeatherHub: estación {estacion}", datos.Estacion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notificando al WeatherHub para la estación {estacion}", datos.Estacion);
            }
        }
    }
}
