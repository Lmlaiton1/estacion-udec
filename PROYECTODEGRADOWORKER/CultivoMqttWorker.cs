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
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ServicioSensorica.Worker
{
    // Control de umbrales y alarmas de cultivo. No modifica Worker.cs ni WeatherMqttWorker.cs.
    //
    // Contrato MQTT (broker local, sin internet):
    //   ESP -> cultivo/{N}/datos                     {"temperatura": 24.1, "humedad": 71.3}
    //          (las llaves son la "Clave" de cada sensor registrado en el cultivo)
    //   PC  -> cultivo/{N}/actuadores/{clave}/cmd    {"accion":"ON"|"OFF","origen":"Automatico"|"Manual"}  (retained)
    //   PC  -> cultivo/{N}/alarma                    {"accion":"ON","nivel":"Alta","duracionSeg":10}
    public class CultivoMqttWorker : BackgroundService
    {
        private static readonly Regex TopicDatos = new(@"^cultivo/(\d+)/datos$", RegexOptions.Compiled);

        private readonly ILogger<CultivoMqttWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly SmsGsmSender _sms;

        private IManagedMqttClient? _mqttClient;
        private volatile HubConnection? _hubConnection;
        private int _reconectandoHub;

        private readonly string _mqttBroker;
        private readonly int _mqttPort;
        private readonly string _cultivoHubUrl;
        private readonly int _sirenaDuracionSeg;

        public CultivoMqttWorker(ILogger<CultivoMqttWorker> logger, IServiceScopeFactory scopeFactory, IConfiguration configuration)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _sms = new SmsGsmSender(configuration, logger);

            _mqttBroker = configuration["Mqtt:Broker"] ?? "localhost";
            _mqttPort = int.TryParse(configuration["Mqtt:Port"], out var port) ? port : 1883;
            _cultivoHubUrl = configuration["CultivoHub:Url"] ?? "http://localhost:5005/Hubs/Cultivo";
            _sirenaDuracionSeg = int.TryParse(configuration["Alarma:DuracionSeg"], out var dur) ? dur : 10;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            IniciarConexionHubSiNecesario(stoppingToken);

            _mqttClient = await CrearClienteMqttAsync(stoppingToken);

            _logger.LogInformation("CultivoMqttWorker iniciado. Escuchando cultivo/+/datos en {broker}:{port}. SMS {estado}",
                _mqttBroker, _mqttPort, _sms.Habilitado ? $"habilitado ({_sms.Destinatarios.Count} destinatarios)" : "deshabilitado");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (_hubConnection == null || _hubConnection.State == HubConnectionState.Disconnected)
                    {
                        IniciarConexionHubSiNecesario(stoppingToken);
                    }

                    await EscalarAlarmasASmsAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error en el ciclo de supervisión del control de cultivo");
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
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
            var client = new MqttFactory().CreateManagedMqttClient();

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
                _logger.LogInformation("Control de cultivo conectado al broker MQTT {broker}:{port}", _mqttBroker, _mqttPort);
                return Task.CompletedTask;
            };

            client.SynchronizingSubscriptionsFailedAsync += e =>
            {
                _logger.LogError(e.Exception, "El broker rechazó la suscripción a cultivo/+/datos");
                return Task.CompletedTask;
            };

            var clientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(_mqttBroker, _mqttPort)
                .WithClientId($"ServicioSensorica-Cultivo-{Environment.MachineName}")
                .Build();

            var managedOptions = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(clientOptions)
                .Build();

            await client.SubscribeAsync(new[]
            {
                new MqttTopicFilterBuilder().WithTopic("cultivo/+/datos").Build()
            });

            await client.StartAsync(managedOptions);
            return client;
        }

        private bool IniciarConexionHubSiNecesario(CancellationToken stoppingToken)
        {
            if (Interlocked.CompareExchange(ref _reconectandoHub, 1, 0) != 0)
                return false;

            _ = ConectarHubEnBackgroundAsync(stoppingToken);
            return true;
        }

        private async Task ConectarHubEnBackgroundAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var connection = new HubConnectionBuilder()
                            .WithUrl(_cultivoHubUrl)
                            .WithAutomaticReconnect()
                            .Build();

                        // Órdenes manuales que llegan desde la página: se reenvían al ESP por MQTT
                        connection.On<ComandoActuadorDto>("ComandoActuadorCultivo", async comando =>
                        {
                            await PublicarComandoActuadorAsync(comando.CultivoNumero, comando.Clave, comando.Accion == "ON", comando.Origen);
                        });

                        await connection.StartAsync(stoppingToken);
                        _hubConnection = connection;
                        _logger.LogInformation("Conectado al CultivoHub en {url}", _cultivoHubUrl);
                        return;
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogError(ex, "Error conectando con CultivoHub. Reintentando en 5 segundos...");
                        await Task.Delay(5000, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Cancelado durante el shutdown del worker
            }
            finally
            {
                Interlocked.Exchange(ref _reconectandoHub, 0);
            }
        }

        private async Task ProcesarMensajeAsync(string topic, string payload, CancellationToken stoppingToken)
        {
            var match = TopicDatos.Match(topic);
            if (!match.Success) return;

            var numeroCultivo = int.Parse(match.Groups[1].Value);
            var ahora = DateTime.Now;

            Dictionary<string, decimal> valores;
            try
            {
                valores = LeerValores(payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payload inválido en {topic}: {payload}", topic, payload);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CultivoDbContext>();

            var cultivo = await db.Cultivos.FirstOrDefaultAsync(c => c.Numero == numeroCultivo && c.Activo, stoppingToken);
            if (cultivo == null)
            {
                _logger.LogWarning("Cultivo {numero} no está registrado o está inactivo, se descartan sus lecturas", numeroCultivo);
                return;
            }

            var sensores = await db.Sensores.Where(s => s.CultivoId == cultivo.Id && s.Activo).ToListAsync(stoppingToken);

            var lecturas = new List<(SensorCultivoW Sensor, LecturaCultivoW Lectura)>();
            foreach (var sensor in sensores)
            {
                if (!valores.TryGetValue(sensor.Clave, out var valor)) continue;

                lecturas.Add((sensor, new LecturaCultivoW
                {
                    Id = Guid.NewGuid(),
                    CultivoId = cultivo.Id,
                    SensorCultivoId = sensor.Id,
                    Variable = sensor.Variable,
                    Valor = valor,
                    Unidad = sensor.Unidad,
                    FechaHora = ahora
                }));
            }

            if (lecturas.Count == 0)
            {
                _logger.LogWarning("Ninguna llave del payload de {topic} coincide con un sensor activo del cultivo", topic);
                return;
            }

            db.Lecturas.AddRange(lecturas.Select(l => l.Lectura));
            await db.SaveChangesAsync(stoppingToken);

            await InvocarHubAsync("NotificarLecturaCultivo", new
            {
                cultivoId = cultivo.Id,
                cultivoNumero = cultivo.Numero,
                fechaHora = ahora,
                lecturas = lecturas.Select(l => new { sensorId = l.Sensor.Id, variable = l.Sensor.Variable, valor = l.Lectura.Valor, unidad = l.Sensor.Unidad })
            });

            try
            {
                await EvaluarUmbralesAsync(db, cultivo, lecturas, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluando umbrales del cultivo {numero}", cultivo.Numero);
            }
        }

        // Solo se evalúan los umbrales sin etapa o de la etapa activa del cultivo
        private async Task EvaluarUmbralesAsync(CultivoDbContext db, CultivoW cultivo,
            List<(SensorCultivoW Sensor, LecturaCultivoW Lectura)> lecturas, CancellationToken ct)
        {
            var etapaActiva = await db.Etapas.FirstOrDefaultAsync(e => e.CultivoId == cultivo.Id && e.Activa, ct);
            var etapaId = etapaActiva?.Id;

            var umbrales = await db.Umbrales
                .Where(u => u.CultivoId == cultivo.Id && u.Activo && (u.EtapaCultivoId == null || u.EtapaCultivoId == etapaId))
                .ToListAsync(ct);
            if (umbrales.Count == 0) return;

            var alarmasNuevas = new List<AlarmaCultivoW>();
            var comandos = new List<(ActuadorCultivoW Actuador, string Motivo)>();

            foreach (var (sensor, lectura) in lecturas)
            {
                foreach (var umbral in umbrales.Where(u => u.SensorCultivoId == sensor.Id))
                {
                    var bajo = umbral.ValorMin.HasValue && lectura.Valor < umbral.ValorMin.Value;
                    var alto = umbral.ValorMax.HasValue && lectura.Valor > umbral.ValorMax.Value;
                    var actuador = umbral.ActuadorCultivoId.HasValue
                        ? await db.Actuadores.FirstOrDefaultAsync(a => a.Id == umbral.ActuadorCultivoId, ct)
                        : null;

                    if (bajo || alto)
                    {
                        var limite = bajo ? $"por debajo del mínimo {umbral.ValorMin:0.##}" : $"por encima del máximo {umbral.ValorMax:0.##}";
                        var alarma = await RegistrarFueraDeRangoAsync(db, cultivo, sensor, umbral, etapaActiva, lectura.Valor, limite, ct);
                        if (alarma.Nueva) alarmasNuevas.Add(alarma.Alarma);

                        // Acción automática: solo si el actuador está en modo Automático y no está ya en el estado pedido
                        if (actuador != null && actuador.Activo && actuador.Modo == "Automatico" &&
                            umbral.EncenderActuador.HasValue && actuador.Encendido != umbral.EncenderActuador.Value)
                        {
                            var motivo = $"{sensor.Variable} = {lectura.Valor:0.##} {sensor.Unidad}, {limite}" +
                                         (etapaActiva != null ? $" (etapa {etapaActiva.Nombre})" : string.Empty);
                            CambiarActuador(db, actuador, umbral.EncenderActuador.Value, motivo, alarma.Alarma.Id, umbral.Id);
                            db.BitacoraAlarmas.Add(new BitacoraAlarmaCultivoW
                            {
                                Id = Guid.NewGuid(),
                                AlarmaCultivoId = alarma.Alarma.Id,
                                Accion = "AccionAutomatica",
                                Comentario = $"{(umbral.EncenderActuador.Value ? "Encendido" : "Apagado")} {actuador.Nombre}",
                                Fecha = DateTime.Now
                            });
                            comandos.Add((actuador, motivo));
                        }
                    }
                    else if (actuador != null && umbral.RevertirAlNormalizar && umbral.EncenderActuador.HasValue &&
                             actuador.Activo && actuador.Modo == "Automatico" && actuador.Encendido == umbral.EncenderActuador.Value)
                    {
                        // Solo se revierte si el último cambio lo hizo este mismo umbral de forma automática
                        var ultimoEvento = await db.Eventos
                            .Where(e => e.DispositivoId == actuador.Id && (e.Accion == "Encendido" || e.Accion == "Apagado"))
                            .OrderByDescending(e => e.Fecha)
                            .FirstOrDefaultAsync(ct);

                        if (ultimoEvento != null && ultimoEvento.Origen == "Automatico" && ultimoEvento.UmbralCultivoId == umbral.Id)
                        {
                            var motivo = $"{sensor.Variable} volvió a rango: {lectura.Valor:0.##} {sensor.Unidad}";
                            CambiarActuador(db, actuador, !umbral.EncenderActuador.Value, motivo, ultimoEvento.AlarmaCultivoId, umbral.Id);
                            comandos.Add((actuador, motivo));
                        }
                    }
                }
            }

            await db.SaveChangesAsync(ct);

            foreach (var (actuador, _) in comandos)
            {
                await PublicarComandoActuadorAsync(cultivo.Numero, actuador.Clave, actuador.Encendido, "Automatico");
                await InvocarHubAsync("NotificarActuadorCultivo", new
                {
                    actuadorId = actuador.Id,
                    cultivoId = actuador.CultivoId,
                    encendido = actuador.Encendido,
                    modo = actuador.Modo,
                    activo = actuador.Activo
                });
            }

            foreach (var alarma in alarmasNuevas)
            {
                await InvocarHubAsync("NotificarAlarmaCultivo", new
                {
                    tipo = "Nueva",
                    id = alarma.Id,
                    cultivoId = alarma.CultivoId,
                    cultivoNombre = cultivo.Nombre,
                    variable = alarma.Variable,
                    valor = alarma.ValorRegistrado,
                    nivelGravedad = alarma.NivelGravedad,
                    mensaje = alarma.Mensaje,
                    estado = alarma.Estado,
                    fechaDisparo = alarma.FechaDisparo
                });
            }

            if (alarmasNuevas.Count > 0)
            {
                var nivel = alarmasNuevas.OrderByDescending(a => PesoGravedad(a.NivelGravedad)).First().NivelGravedad;
                await PublicarAsync($"cultivo/{cultivo.Numero}/alarma",
                    JsonSerializer.Serialize(new { accion = "ON", nivel, duracionSeg = _sirenaDuracionSeg }), retener: false);
                _logger.LogWarning("Generadas {count} alarmas en el cultivo {numero}", alarmasNuevas.Count, cultivo.Numero);
            }
        }

        // Una alarma abierta por umbral: si ya hay una sin resolver, solo suma la ocurrencia
        private async Task<(AlarmaCultivoW Alarma, bool Nueva)> RegistrarFueraDeRangoAsync(CultivoDbContext db, CultivoW cultivo,
            SensorCultivoW sensor, UmbralCultivoW umbral, EtapaCultivoW? etapa, decimal valor, string limite, CancellationToken ct)
        {
            var ahora = DateTime.Now;

            var abierta = await db.Alarmas
                .Where(a => a.UmbralCultivoId == umbral.Id && !a.Eliminado && a.Estado != "Resuelta" && a.Estado != "Anulada")
                .OrderByDescending(a => a.FechaDisparo)
                .FirstOrDefaultAsync(ct);

            if (abierta != null)
            {
                abierta.UltimoValor = valor;
                abierta.Ocurrencias++;
                abierta.FechaUltimaOcurrencia = ahora;
                return (abierta, false);
            }

            var mensaje = $"{cultivo.Nombre}: {sensor.Variable} = {valor:0.##} {sensor.Unidad}, {limite}" +
                          (etapa != null ? $" (etapa {etapa.Nombre})" : string.Empty);

            var nueva = new AlarmaCultivoW
            {
                Id = Guid.NewGuid(),
                CultivoId = cultivo.Id,
                UmbralCultivoId = umbral.Id,
                SensorCultivoId = sensor.Id,
                EtapaNombre = etapa?.Nombre,
                Variable = sensor.Variable,
                ValorRegistrado = valor,
                UltimoValor = valor,
                Ocurrencias = 1,
                Mensaje = mensaje.Length > 500 ? mensaje[..500] : mensaje,
                NivelGravedad = umbral.NivelGravedad,
                Estado = "Activa",
                FechaDisparo = ahora,
                FechaUltimaOcurrencia = ahora,
                SmsEnviado = false
            };

            db.Alarmas.Add(nueva);
            db.BitacoraAlarmas.Add(new BitacoraAlarmaCultivoW
            {
                Id = Guid.NewGuid(),
                AlarmaCultivoId = nueva.Id,
                Accion = "Disparada",
                Comentario = nueva.Mensaje,
                Fecha = ahora
            });

            return (nueva, true);
        }

        private static void CambiarActuador(CultivoDbContext db, ActuadorCultivoW actuador, bool encender, string motivo, Guid? alarmaId, Guid umbralId)
        {
            actuador.Encendido = encender;
            actuador.FechaUltimoCambio = DateTime.Now;

            db.Eventos.Add(new EventoDispositivoCultivoW
            {
                Id = Guid.NewGuid(),
                CultivoId = actuador.CultivoId,
                TipoDispositivo = "Actuador",
                DispositivoId = actuador.Id,
                NombreDispositivo = actuador.Nombre,
                Accion = encender ? "Encendido" : "Apagado",
                Origen = "Automatico",
                Motivo = motivo.Length > 500 ? motivo[..500] : motivo,
                UsuarioId = null,
                AlarmaCultivoId = alarmaId,
                UmbralCultivoId = umbralId,
                Fecha = DateTime.Now
            });
        }

        // Segundo nivel de aviso: si nadie marcó la alarma como vista en el tiempo del umbral, SMS.
        private async Task EscalarAlarmasASmsAsync(CancellationToken ct)
        {
            if (!_sms.Habilitado) return;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CultivoDbContext>();
            var ahora = DateTime.Now;

            var pendientes = await (from a in db.Alarmas
                                    join u in db.Umbrales on a.UmbralCultivoId equals u.Id
                                    where a.Estado == "Activa" && !a.Eliminado && !a.SmsEnviado && u.MinutosEscalamientoSms != null
                                    select new { Alarma = a, Minutos = u.MinutosEscalamientoSms!.Value })
                                   .ToListAsync(ct);

            foreach (var p in pendientes.Where(p => p.Alarma.FechaDisparo.AddMinutes(p.Minutos) <= ahora))
            {
                var texto = $"ALARMA {p.Alarma.NivelGravedad}: {p.Alarma.Mensaje}. Sin atender hace {p.Minutos} min.";
                var enviados = await _sms.EnviarATodosAsync(texto);

                db.BitacoraAlarmas.Add(new BitacoraAlarmaCultivoW
                {
                    Id = Guid.NewGuid(),
                    AlarmaCultivoId = p.Alarma.Id,
                    Accion = enviados > 0 ? "SmsEnviado" : "SmsFallido",
                    Comentario = enviados > 0
                        ? $"SMS enviado a {enviados} de {_sms.Destinatarios.Count} destinatarios"
                        : "No se pudo enviar el SMS (revisar módem GSM, señal o saldo)",
                    Fecha = DateTime.Now
                });

                // Se marca aunque falle para no reintentar cada 30 s; el fallo queda en la bitácora
                p.Alarma.SmsEnviado = true;
                p.Alarma.FechaSms = DateTime.Now;
            }

            await db.SaveChangesAsync(ct);
        }

        // Retained: si el ESP se reinicia, al reconectarse recibe el último estado ordenado
        private Task PublicarComandoActuadorAsync(int cultivoNumero, string clave, bool encender, string origen)
        {
            var payload = JsonSerializer.Serialize(new { accion = encender ? "ON" : "OFF", origen });
            return PublicarAsync($"cultivo/{cultivoNumero}/actuadores/{clave}/cmd", payload, retener: true);
        }

        private async Task PublicarAsync(string topic, string payload, bool retener)
        {
            if (_mqttClient == null) return;

            try
            {
                var mensaje = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(payload)
                    .WithRetainFlag(retener)
                    .Build();
                await _mqttClient.EnqueueAsync(mensaje);
                _logger.LogInformation("MQTT publicado en {topic}: {payload}", topic, payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publicando en {topic}", topic);
            }
        }

        private async Task InvocarHubAsync(string metodo, object datos)
        {
            if (_hubConnection == null || _hubConnection.State != HubConnectionState.Connected) return;

            try
            {
                await _hubConnection.InvokeAsync(metodo, datos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notificando {metodo} al CultivoHub", metodo);
            }
        }

        // Acepta números y strings numéricos; ignora el resto (ej. "fecha")
        private static Dictionary<string, decimal> LeerValores(string payload)
        {
            var resultado = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            using var doc = JsonDocument.Parse(payload);

            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.Number && prop.Value.TryGetDecimal(out var numero))
                    resultado[prop.Name] = numero;
                else if (prop.Value.ValueKind == JsonValueKind.String &&
                         decimal.TryParse(prop.Value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var texto))
                    resultado[prop.Name] = texto;
            }

            return resultado;
        }

        private static int PesoGravedad(string? nivel) => nivel?.Trim().ToLowerInvariant() switch
        {
            "alta" => 3,
            "media" => 2,
            _ => 1
        };

        public class ComandoActuadorDto
        {
            public Guid ActuadorId { get; set; }
            public int CultivoNumero { get; set; }
            public string Clave { get; set; } = string.Empty;
            public string Accion { get; set; } = string.Empty;
            public string Origen { get; set; } = string.Empty;
        }
    }
}
