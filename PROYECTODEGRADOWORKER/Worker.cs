using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ServicioSensorica.Data;
using ServicioSensorica.Models;
using System;
using System.Globalization;
using System.IO.Ports;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ServicioSensorica.Worker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private SerialPort? _serialPort;
        private HubConnection _connection;
        private HubConnection _dosificadorConnection;
        private HubConnection _sdConnection;
        private bool _transferenciaEnCurso = false;

        private Dictionary<string, int> actuadores = new()
        {
            { "Activo", 0 }
        };

        private readonly Dictionary<Guid, DateTime> _ultimaEjecucion = new();

        public Worker(ILogger<Worker> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            // Crear conexión de actuadores con reconexión automática
            var connection = await CrearConexionSignalR(stoppingToken);

            // Crear conexión del nuevo Hub de dosificadores con reconexión automática
            _dosificadorConnection = await CrearConexionDosificadores(stoppingToken);

            _sdConnection = await CrearConexionSD(stoppingToken);

            // 4. Tu bucle principal original
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (connection.State == HubConnectionState.Disconnected)
                    {
                        _logger.LogWarning("SignalR (Actuators) desconectado, intentando reconectar...");
                        connection = await CrearConexionSignalR(stoppingToken);
                    }

                    if (_dosificadorConnection.State == HubConnectionState.Disconnected)
                    {
                        _logger.LogWarning("SignalR (Dosificadores) desconectado, intentando reconectar...");
                        _dosificadorConnection = await CrearConexionDosificadores(stoppingToken);
                    }

                    if (_sdConnection.State == HubConnectionState.Disconnected)
                    {
                        _logger.LogWarning("SignalR (Transferencia SD) desconectado, intentando reconectar...");
                        _sdConnection = await CrearConexionSD(stoppingToken);
                    }

                    if (_serialPort == null || !_serialPort.IsOpen)
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                        var config = await db.SerialPortConfigs.FirstOrDefaultAsync(stoppingToken);

                        if (config == null)
                        {
                            _logger.LogWarning("⚠️ No se encontró configuración del puerto serial en la base de datos.");
                            await Task.Delay(5000, stoppingToken);
                            continue; 
                        }

                        try
                        {
                            _serialPort = new SerialPort(config.PortName, config.BaudRate)
                            {
                                NewLine = "\r\n",
                                ReadTimeout = 30000
                            };

                            _serialPort.Open();
                            _logger.LogInformation("✅ Puerto serial conectado en {port} a {baud} baudios.", config.PortName, config.BaudRate);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "❌ Error al conectar con el puerto serial {port} a {baud}", config.PortName, config.BaudRate);
                            CerrarPuerto();
                            await Task.Delay(5000, stoppingToken);
                            continue;
                         }
                    }

                    _ = Task.Run(async () =>
                    {
                        while (!stoppingToken.IsCancellationRequested)
                        {
                            try
                            {
                                using var scope = _scopeFactory.CreateScope();
                                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                                var now = DateTime.Now;
                                int currentHour = now.Hour;
                                int currentMinute = now.Minute;

                                var schedules = await db.Schedules.ToListAsync(stoppingToken);

                                foreach (var schedule in schedules)
                                {
                                    // ✅ Comprobamos si la hora y minuto coinciden
                                    if (schedule.Hour == currentHour && schedule.Minute == currentMinute)
                                    {
                                        // ✅ Verificamos si ya se ejecutó hoy en ese minuto
                                        if (!_ultimaEjecucion.ContainsKey(schedule.Id) ||
                                            _ultimaEjecucion[schedule.Id].Date != now.Date ||
                                            _ultimaEjecucion[schedule.Id].Hour != currentHour ||
                                            _ultimaEjecucion[schedule.Id].Minute != currentMinute)
                                        {
                                            var device = await db.Devices.FirstOrDefaultAsync(s => s.Id == schedule.DeviceId, stoppingToken);
                                            if (device == null)
                                            {
                                                _logger.LogWarning("⚠ Dispositivo con ID {id} no encontrado para el horario {scheduleId}", schedule.DeviceId, schedule.Id);
                                                continue;
                                            }

                                            _logger.LogInformation("⏰ Ejecutando dosificación para {device} a las {hour:D2}:{minute:D2}",
                                                device.Device_Name, currentHour, currentMinute);

                                            if (_serialPort?.IsOpen == true)
                                            {
                                                string comando = $"{device.Device_Name}:ON:{schedule.DurationSeconds}";
                                                _serialPort.WriteLine(comando);
                                                await updateState(device.Device_Name, "Activo", stoppingToken);

                                                _logger.LogInformation("✅ Comando enviado: {cmd}", comando);
                                            }

                                            // ✅ Registramos el momento exacto en que se ejecutó
                                            _ultimaEjecucion[schedule.Id] = now;
                                        }
                                        else
                                        {
                                            _logger.LogInformation("⏳ Ya se ejecutó la dosificación para {device} en este minuto, se omitirá.", schedule.DeviceId);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "❌ Error verificando horarios de dosificación");
                            }

                            // Esperamos 1 minuto antes de volver a revisar
                            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                        }
                    }, stoppingToken);

                    // Lanzar PING en paralelo
                    _ = Task.Run(async () =>
                    {
                        while (!stoppingToken.IsCancellationRequested && _serialPort?.IsOpen == true)
                        {
                            try
                            {
                                _serialPort.WriteLine("PING");
                                _logger.LogInformation("PING enviado al Arduino");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error enviando PING");
                            }

                            await Task.Delay(10000, stoppingToken);  // 10 segundos
                        }
                    }, stoppingToken);

                    // Bucle de lectura de mediciones
                    while (!stoppingToken.IsCancellationRequested && _serialPort?.IsOpen == true)
                    {
                        try
                        {
                            _logger.LogInformation("Esperando datos...");
                            string? json = _serialPort.ReadLine();
                            _logger.LogInformation("Datos crudos recibidos: {json}", json);

                            if (!string.IsNullOrWhiteSpace(json))
                            {
                                // Si empieza con CAMBIO: es mensaje de cambio de estado
                                if (json.StartsWith("CAMBIO:"))
                                {
                                    await ProcessCambioAsync(json, stoppingToken);
                                }
                                else
                                {
                                    await ProcessJsonAsync(json, stoppingToken);
                                }
                            }
                        }
                        catch (TimeoutException)
                        {
                            _logger.LogWarning("Timeout esperando datos desde el puerto serial...");
                        }
                        catch (IOException)
                        {
                            _logger.LogWarning("Conexión serial perdida. Intentando reconectar...");
                            CerrarPuerto();
                            break; 
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error leyendo datos del puerto serial");
                        }

                        await Task.Delay(200, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error general en ExecuteAsync");
                    CerrarPuerto();
                    await Task.Delay(3000, stoppingToken);
                }
            }
        }

        private async Task<HubConnection> CrearConexionSD(CancellationToken stoppingToken)
        {
            HubConnection? connection = null;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    connection = new HubConnectionBuilder()
                        .WithUrl("http://localhost:5005/Hubs/SdTransfer")
                        .WithAutomaticReconnect()
                        .Build();

                    await connection.StartAsync(stoppingToken);
                    _logger.LogInformation("Conectado al Hub de Transferencia SD");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error conectando con Hub de Transferencia SD. Reintentando en 5 segundos...");
                    await Task.Delay(5000, stoppingToken);
                }
            }

            return connection!;
        }


        private async Task<HubConnection> CrearConexionDosificadores(CancellationToken stoppingToken)
        {
            HubConnection? connection = null;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    connection = new HubConnectionBuilder()
                        .WithUrl("http://localhost:5005/Hubs/State")
                        .WithAutomaticReconnect()
                        .Build();

                    await connection.StartAsync(stoppingToken);
                    _logger.LogInformation("✅ Conectado al Hub de Dosificadores");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error conectando con Hub de Dosificadores. Reintentando en 5 segundos...");
                    await Task.Delay(5000, stoppingToken);
                }
            }

            return connection!;
        }

        private async Task<HubConnection> CrearConexionSignalR(CancellationToken stoppingToken)
        {
            HubConnection? connection = null;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    connection = new HubConnectionBuilder()
                        .WithUrl("http://localhost:5005/Hubs/Actuators")
                        .WithAutomaticReconnect()
                        .Build();

                    connection.On<string, string>("ReceiveCommand", async (device, state) =>
                    {
                        _logger.LogInformation("Comando recibido desde backend: {device} -> {state}", device, state);
                        await EjecutarComando(device, state, stoppingToken);
                    });

                    await connection.StartAsync(stoppingToken);
                    _logger.LogInformation("Conectado a SignalR en backend");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error conectando con SignalR. Reintentando en 5 segundos...");
                    await Task.Delay(5000, stoppingToken); 
                }
            }

            return connection!;
        }


        private async Task ProcessCambioAsync(string message, CancellationToken stoppingToken)
        {
            try
            {
                var Notify1 = new Guid();
                var partes = message.Split(':');
                if (partes.Length != 3)
                {
                    _logger.LogWarning("Formato inválido para mensaje de cambio: {message}", message);
                    return;
                }

                string actuador = partes[1];
                string estado = partes[2];

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Buscar dispositivo
                var device = await db.Devices.FirstOrDefaultAsync(d => d.Device_Name == actuador, stoppingToken);
                if (device == null)
                {
                    _logger.LogWarning("Dispositivo {actuador} no encontrado en BD.", actuador);
                    return;
                }

                // Buscar estado en Device_Status
                var status = await db.Device_States.FirstOrDefaultAsync(s => s.State_Name == estado, stoppingToken);
                if (status == null)
                {
                    _logger.LogWarning("Estado {estado} no encontrado en BD.", estado);
                    return;
                }

                // Actualizar estado
                device.Device_StatusId = status.Id;
                db.Devices.Update(device);

                var impactType = await db.Impacts.FirstOrDefaultAsync(i => i.Impact_Type == "Nulo", stoppingToken);
                var systemType = await db.System_Types.FirstOrDefaultAsync(s => s.Id == device.System_TypeId, stoppingToken);

                if (impactType != null && systemType != null)
                {
                    var newEvent = new Event
                    {
                        Id = Guid.NewGuid(),
                        Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        Notification = $"El {device.Device_Name} fue: {status.State_Name}",
                        Visto = false,
                        DeviceId = device.Id,
                        ImpactId = impactType.Id,
                        System_TypeId = systemType.Id
                    };
                    db.Events.Add(newEvent);
                    Notify1 = newEvent.Id;
                }

                await db.SaveChangesAsync(stoppingToken);

                if (_dosificadorConnection != null && _dosificadorConnection.State == HubConnectionState.Connected)
                {
                    await _dosificadorConnection.InvokeAsync(
                        "NotificarEstadoDosificador",
                        device.Device_Name,
                        status.State_Name,
                        cancellationToken: stoppingToken
                    );

                    _logger.LogInformation("Notificado al Hub de Dosificadores: {actuador} -> {estado}", device.Device_Name, status.State_Name);
                }

                await NotificarBackend(Notify1);

                _logger.LogInformation("Estado de {actuador} actualizado a {estado}.", actuador, estado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando cambio de estado: {message}", message);
            }
        }

        private void CerrarPuerto()
        {
            try
            {
                if (_serialPort != null)
                {
                    if (_serialPort.IsOpen)
                        _serialPort.Close();
                    _serialPort.Dispose();
                }
            }
            catch { }
            finally
            {
                _serialPort = null;
            }
        }

             

        private async Task ProcessJsonAsync(string json, CancellationToken stoppingToken)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Array)
                {
                    if (!_transferenciaEnCurso)
                    {
                        _transferenciaEnCurso = true;

                        if (_sdConnection != null && _sdConnection.State == HubConnectionState.Connected)
                        {
                            await _sdConnection.InvokeAsync("NotificarEstadoTransferencia", "Iniciando", cancellationToken: stoppingToken);
                            _logger.LogInformation("Iniciando transferencia completa desde SD...");
                        }
                    }
                    // Si es un array, procesar cada objeto en él
                    foreach (var element in root.EnumerateArray())
                    {
                        await ProcessSingleMeasurementAsync(element, stoppingToken);
                    }
                }
                else if (root.ValueKind == JsonValueKind.Object)
                {
                    if (_transferenciaEnCurso)
                    {
                        if (_sdConnection != null && _sdConnection.State == HubConnectionState.Connected)
                        {
                            await _sdConnection.InvokeAsync("NotificarEstadoTransferencia", "Finalizado", cancellationToken: stoppingToken);
                            _logger.LogInformation("Transferencia completa desde SD finalizada.");
                        }

                        _transferenciaEnCurso = false;

                        string comando = "RESET:ON";

                        _serialPort.WriteLine(comando);

                    }
                    await ProcessSingleMeasurementAsync(root, stoppingToken);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando JSON recibido: {json}", json);
            }
        }


        private async Task ProcessSingleMeasurementAsync(JsonElement root, CancellationToken stoppingToken)
        {
            var Notify2 = new List<Guid>();

            DateTime fechaHora;
            if (root.TryGetProperty("FechaHora", out JsonElement fechaHoraElem))
            {
                actuadores["Activado"] = 0;
                fechaHora = DateTime.Parse(fechaHoraElem.GetString()!);
            }
            else
            {
                actuadores["Activado"] = 1;
                fechaHora = DateTime.Now;
            }

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            foreach (var property in root.EnumerateObject())
            {
                if (property.Name == "FechaHora") continue;

                string sensorName = property.Name;
                var sensorData = property.Value;

                decimal value = sensorData.GetProperty("valor").GetDecimal();
                string unit = sensorData.GetProperty("unidad").GetString() ?? "";

                var device = await db.Devices.FirstOrDefaultAsync(d => d.Device_Name == sensorName, stoppingToken);
                if (device == null)
                {
                    _logger.LogError($"No se encontró un dispositivo con nombre '{sensorName}' en la BD.");
                    continue;
                }

                var measurementType = await db.Measurement_Types.FirstOrDefaultAsync(u => u.Name == sensorName, stoppingToken);
                if (measurementType == null)
                {
                    _logger.LogError($"No se encontró el tipo de medición '{sensorName}' en la BD.");
                    continue;
                }

                var measurement = new Measurement
                {
                    Id = Guid.NewGuid(),
                    Value = value,
                    Date = fechaHora.ToString("yyyy-MM-dd HH:mm:ss"),
                    DeviceId = device.Id,
                    Measurement_TypeId = measurementType.Id
                };
                db.Measurements.Add(measurement);

                var rule = await db.ControlRules.FirstOrDefaultAsync(r => r.DeviceId == device.Id);
                if (rule != null)
                {
                    var impact = GetImpactForSensor(rule.Max, rule.Min, value);
                    if (impact != null)
                    {
                        var impactType = await db.Impacts.FirstOrDefaultAsync(i => i.Impact_Type == impact, stoppingToken);
                        var systemType = await db.System_Types.FirstOrDefaultAsync(s => s.Id == device.System_TypeId, stoppingToken);

                        if (impactType != null && systemType != null)
                        {
                            var newEvent = new Event
                            {
                                Id = Guid.NewGuid(),
                                Date = fechaHora.ToString("yyyy-MM-dd HH:mm:ss"),
                                Notification = $"{sensorName} fuera de rango: {value}, su impacto es: ({impact})",
                                Visto = false,
                                DeviceId = device.Id,
                                ImpactId = impactType.Id,
                                System_TypeId = systemType.Id
                            };
                            db.Events.Add(newEvent);
                            Notify2.Add(newEvent.Id);
                        }
                    }
                }

                _logger.LogInformation("Sensor {sensor}: {val} {unit} en {fecha}", sensorName, value, unit, fechaHora);
            }

            await db.SaveChangesAsync(stoppingToken);
            foreach (var id in Notify2)
            {
                await NotificarBackend(id);
            }

            if (actuadores["Activado"] == 1)
            {
                foreach (var property in root.EnumerateObject())
                {
                    await actuatorAutomate(property.Name, property.Value.GetProperty("valor").GetDecimal(), stoppingToken);
                }
            }
        }

        
        /*
        private async Task ProcessJsonAsync(string json, CancellationToken stoppingToken)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var Notify2 = new List<Guid>();

                // 1. Revisar si viene fecha/hora en el JSON
                DateTime fechaHora;
                if (root.TryGetProperty("FechaHora", out JsonElement fechaHoraElem))
                {
                    actuadores["Activado"] = 0;
                    fechaHora = DateTime.Parse(fechaHoraElem.GetString()!); // usa la fecha del CSV
                }
                else
                {
                    actuadores["Activado"] = 1;
                    fechaHora = DateTime.Now;
                }

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                foreach (var property in root.EnumerateObject())
                {

                    if (property.Name == "FechaHora") continue; 
                    string sensorName = property.Name;
                    var sensorData = property.Value;

                    decimal value = sensorData.GetProperty("valor").GetDecimal();
                    string unit = sensorData.GetProperty("unidad").GetString() ?? "";

                    var device = await db.Devices.FirstOrDefaultAsync(d => d.Device_Name == sensorName, stoppingToken);

                    if (device == null)
                    {
                        _logger.LogError($"No se encontró un dispositivo con nombre '{sensorName}' en la BD.");
                        continue; // o puedes crear el device si quieres
                    }

                    var measurementType = await db.Measurement_Types.FirstOrDefaultAsync(u => u.Name == sensorName, stoppingToken);

                    if (measurementType == null)
                    {
                        _logger.LogError($"No se encontró el tipo de medicion '{measurementType}' en la BD.");
                        continue; // o puedes crear la unidad
                    }

                    var measurement = new Measurement
                    {
                        Id = Guid.NewGuid(),
                        Value = value,
                        Date = fechaHora.ToString("yyyy-MM-dd HH:mm:ss"),
                        DeviceId = device.Id,
                        Measurement_TypeId = measurementType.Id
                    };

                    db.Measurements.Add(measurement);

                    var rule = await db.ControlRules.FirstOrDefaultAsync(r => r.DeviceId == device.Id);

                    var impact = GetImpactForSensor(rule.Max, rule.Min, value);
                    if (impact != null)
                    {
                        // Buscar ImpactId y System_TypeId en BD
                        var impactType = await db.Impacts.FirstOrDefaultAsync(i => i.Impact_Type == impact, stoppingToken);
                        var systemType = await db.System_Types.FirstOrDefaultAsync(s => s.Id == device.System_TypeId, stoppingToken);

                        if (impactType != null && systemType != null)
                        {
                            var newEvent = new Event
                            {
                                Id = Guid.NewGuid(),
                                Date = fechaHora.ToString("yyyy-MM-dd HH:mm:ss"),
                                Notification = $"{sensorName} fuera de rango: {value}, su impacto es: ({impact})",
                                Visto = false,
                                DeviceId = device.Id,
                                ImpactId = impactType.Id,
                                System_TypeId = systemType.Id
                            };
                            db.Events.Add(newEvent);                            
                            Notify2.Add(newEvent.Id);
                        }
                    }



                    _logger.LogInformation("Sensor {sensor}: {val} {unit} en {fecha}", sensorName, value, unit, fechaHora);
                }

                await db.SaveChangesAsync(stoppingToken);
                foreach (var id in Notify2)
                {
                    await NotificarBackend(id);
                }

                if(actuadores["Activado"] == 1)
                {
                    foreach (var property in root.EnumerateObject())
                    {
                        await actuatorAutomate(property.Name, property.Value.GetProperty("valor").GetDecimal(), stoppingToken);
                    }
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando JSON recibido: {json}", json);
            }
        }*/

        public async Task NotificarBackend(Guid eventId)
        {
            var backendUrl = "http://localhost:5005/api/ApiEvent/Notify";

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            using var client = new HttpClient(handler);
            try
            {
                var response = await client.PostAsJsonAsync(backendUrl, eventId);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Error notificando al backend: {status}", response.StatusCode);
                }
                else
                {
                    _logger.LogInformation("Notificación enviada correctamente para {eventId}", eventId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando notificación al backend");
            }
        }

        private string? GetImpactForSensor(decimal? maxOptimo, decimal? minOptimo, decimal value)
        {
            decimal? rango = maxOptimo - minOptimo;
            decimal ampliacion = 0.4m;

            if (value < minOptimo - (ampliacion * rango) * 2 || value > maxOptimo + (ampliacion * rango) * 2) return "Alto";

            if (value < minOptimo - ampliacion * rango || value > maxOptimo + ampliacion * rango) return "Medio";

            if (value < minOptimo || value > maxOptimo) return "Bajo";

            return null; 
        }

        private async Task updateState(string actuador, string estado, CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                
                var device = await db.Devices.FirstOrDefaultAsync(d => d.Device_Name == actuador, stoppingToken);
                if (device == null)
                {
                    _logger.LogWarning("Dispositivo {actuador} no encontrado en BD.", actuador);
                }

         
                var status = await db.Device_States.FirstOrDefaultAsync(s => s.State_Name == estado, stoppingToken);
                if (status == null)
                {
                    _logger.LogWarning("Estado {estado} no encontrado en BD.", estado);
                }

                device.Device_StatusId = status.Id;
                db.Devices.Update(device);

                await db.SaveChangesAsync(stoppingToken);

                if (_dosificadorConnection != null && _dosificadorConnection.State == HubConnectionState.Connected)
                {
                    await _dosificadorConnection.InvokeAsync(
                        "NotificarEstadoDosificador",
                        device.Device_Name,
                        estado,
                        cancellationToken: stoppingToken
                    );

                    _logger.LogInformation("📡 Notificado inicio al Hub de Dosificadores: {device} -> {state}", device.Device_Name, estado);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando estado actuador en la BD");
            }
            
        }

        private async Task<bool> IsInAutoMode(string actuatorName, CancellationToken token)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var actuator = await db.Devices.FirstOrDefaultAsync(a => a.Device_Name == actuatorName);

            var mode = await db.ActuatorModes
                .Where(m => m.DeviceId == actuator.Id)
                .Select(m => m.IsAutoMode)
                .FirstOrDefaultAsync(token);

            return mode;
        }

        private async Task actuatorAutomate(string sensorName, decimal value, CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var sensorDevice = await db.Devices.AsNoTracking().FirstOrDefaultAsync(d => d.Device_Name == sensorName, stoppingToken);

            if (sensorDevice == null)
            {
                _logger?.LogWarning("Sensor no encontrado: {Sensor}", sensorName);
                return;
            }

            var rules = await db.ControlRules.AsNoTracking().Where(r => r.DeviceId == sensorDevice.Id).ToListAsync(stoppingToken);

            if (rules == null || !rules.Any()) return;

            foreach (var rule in rules)
            {
                var actuators = await (from cra in db.ControlRuleActuators.AsNoTracking()
                                       join d in db.Devices.AsNoTracking() on cra.DeviceId equals d.Id
                                       where cra.ControlRuleId == rule.Id
                                       select new
                                       {
                                           ControlRuleActuatorId = cra.Id,
                                           DeviceId = cra.DeviceId,
                                           TriggerType = cra.TriggerType, 
                                           DeviceName = d.Device_Name
                                       }).ToListAsync(stoppingToken);

                if (actuators == null || !actuators.Any()) continue;

                bool isAbove = rule.Max.HasValue && value > rule.Max.Value;//esta por encima?
                bool isBelow = rule.Min.HasValue && value < rule.Min.Value;//esta por debajo?
                bool isWithin = !isAbove && !isBelow;//esta dentro del rango

                if (isAbove)
                {
                    var toConsiderForOff = new List<dynamic>();

                    foreach (var a in actuators)
                    {
                        var currentState = await GetActuatorState(a.DeviceName, stoppingToken);
                        if (currentState == 1)
                        {
                            toConsiderForOff.Add(a);
                        }
                    }

                    foreach (var a in toConsiderForOff)
                    {

                        bool shouldTurnOff = false;
                        
                        if (string.Equals(a.TriggerType, "Deficit", StringComparison.OrdinalIgnoreCase) && rule.Min.HasValue && value >= rule.Min.Value)
                        {
                            shouldTurnOff = true;
                        }

                        if (!shouldTurnOff) continue;
                        if (!await IsInAutoMode(a.DeviceName, stoppingToken)) continue;

                        await SendCommandAsync(a.DeviceName, "OFF", stoppingToken);
                        await updateState(a.DeviceName, "Inactivo", stoppingToken);
                    }

                    var toActivate = actuators
                        .Where(a => string.Equals(a.TriggerType, "Exceso", StringComparison.OrdinalIgnoreCase)
                                 || string.Equals(a.TriggerType, "Ambos", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    foreach (var a in toActivate)
                    {
                        if (!await IsInAutoMode(a.DeviceName, stoppingToken)) continue;         // respeta modo manual/auto
                        if (await GetActuatorState(a.DeviceName, stoppingToken) == 1) continue;           // ya está ON

                        await SendCommandAsync(a.DeviceName, "ON", stoppingToken);
                        //actuadores[a.DeviceName] = 1;                                // actualizar estado en memoria
                        await updateState(a.DeviceName, "Activo", stoppingToken);               // actualizar BD / historial
                    }
                }
                else if (isBelow)
                {
                    var toConsiderForOff = new List<dynamic>();

                    foreach (var a in actuators)
                    {
                        var currentState = await GetActuatorState(a.DeviceName, stoppingToken);
                        if (currentState == 1)
                        {
                            toConsiderForOff.Add(a);
                        }
                    }

                    foreach (var a in toConsiderForOff)
                    {

                        bool shouldTurnOff = false;

                        if (string.Equals(a.TriggerType, "Exceso", StringComparison.OrdinalIgnoreCase) && rule.Min.HasValue && value >= rule.Min.Value)
                        {
                            shouldTurnOff = true;
                        }

                        if (!shouldTurnOff) continue;
                        if (!await IsInAutoMode(a.DeviceName, stoppingToken)) continue;

                        await SendCommandAsync(a.DeviceName, "OFF", stoppingToken);
                        await updateState(a.DeviceName, "Inactivo", stoppingToken);
                    }

                    var toActivate = actuators
                        .Where(a => string.Equals(a.TriggerType, "Deficit", StringComparison.OrdinalIgnoreCase)
                                 || string.Equals(a.TriggerType, "Ambos", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    foreach (var a in toActivate)
                    {
                        if (!await IsInAutoMode(a.DeviceName, stoppingToken)) continue;
                        if (await GetActuatorState(a.DeviceName, stoppingToken) == 1) continue;

                        await SendCommandAsync(a.DeviceName, "ON", stoppingToken);
                        actuadores[a.DeviceName] = 1;
                        await updateState(a.DeviceName, "Activo", stoppingToken);
                    }
                }

                if (isWithin)
                {
                    var toConsiderForOff = new List<dynamic>(); 

                    foreach (var a in actuators)
                    {
                        var currentState = await GetActuatorState(a.DeviceName, stoppingToken);
                        if (currentState == 1) { 
                            toConsiderForOff.Add(a);
                        }
                    }

                    foreach (var a in toConsiderForOff)
                    {

                        bool shouldTurnOff = false;
                        if (string.Equals(a.TriggerType, "Ambos", StringComparison.OrdinalIgnoreCase))
                        {
                            shouldTurnOff = true;
                        }
                        else if (string.Equals(a.TriggerType, "Exceso", StringComparison.OrdinalIgnoreCase) && rule.Max.HasValue && value <= rule.Max.Value)
                        {
                            shouldTurnOff = true;
                        }
                        else if (string.Equals(a.TriggerType, "Deficit", StringComparison.OrdinalIgnoreCase) && rule.Min.HasValue && value >= rule.Min.Value)
                        {
                            shouldTurnOff = true;
                        }

                        if (!shouldTurnOff) continue;
                        if (!await IsInAutoMode(a.DeviceName, stoppingToken)) continue;

                        await SendCommandAsync(a.DeviceName, "OFF", stoppingToken);
                        await updateState(a.DeviceName, "Inactivo", stoppingToken);
                    }
                }
            } 
        }

        private async Task<int> GetActuatorState(string deviceName, CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var device = await db.Devices.AsNoTracking().FirstOrDefaultAsync(d => d.Device_Name == deviceName, stoppingToken);

            var state = await db.Device_States.AsNoTracking().FirstOrDefaultAsync(d => d.Id == device.Device_StatusId, stoppingToken);

            if (state.State_Name == "Activo") return 1;

            return 0;
        }

        private async Task SendCommandAsync(string deviceName, string state, CancellationToken ct)
        {
            var command = $"{deviceName.ToUpper()}:{state.ToUpper()}";
            _serialPort.WriteLine(command);
            await Task.Delay(100, ct); 
        }

        private async Task EjecutarComando(string device, string state, CancellationToken stoppingToken)
        {
            try
            {
                if (_serialPort == null || !_serialPort.IsOpen)
                {
                    _logger.LogError("Puerto serial no disponible para ejecutar comando");
                    return;
                }

                string comando = $"{device}:{(state == "Activo" ? "ON" : "OFF")}";

                _serialPort.WriteLine(comando);
                if(device != "RESET") await updateState(device, state, stoppingToken);

                _logger.LogInformation("Comando {comando} ejecutado en {device}", comando, device);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ejecutando comando para {device}", device);
            }
        }

    }
}
