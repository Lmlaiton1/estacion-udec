using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RR_Nueva_Naturaleza.DAL;
using RR_Nueva_Naturaleza.Hubs;
using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Models.Dto;

namespace RR_Nueva_Naturaleza.Service
{
    public class ActuadorCultivoService : IActuadorCultivoService
    {
        private readonly RR_Nueva_NaturalezaContext _context;
        private readonly IBitacoraCultivoService _bitacora;
        private readonly IHubContext<CultivoHub> _hub;
        private readonly IHubContext<ActuatorHub> _actuatorHub;

        public ActuadorCultivoService(RR_Nueva_NaturalezaContext context, IBitacoraCultivoService bitacora, IHubContext<CultivoHub> hub,
            IHubContext<ActuatorHub> actuatorHub)
        {
            _context = context;
            _bitacora = bitacora;
            _hub = hub;
            _actuatorHub = actuatorHub;
        }

        public async Task<IEnumerable<ActuadorCultivo>> GetActuadores(Guid? cultivoId)
        {
            var query = _context.Actuadores_Cultivo.Include(a => a.Cultivo).Where(a => !a.Eliminado);
            if (cultivoId.HasValue) query = query.Where(a => a.CultivoId == cultivoId.Value);
            return await query.OrderBy(a => a.Nombre).ToListAsync();
        }

        public async Task<ActuadorCultivo?> GetActuador(Guid id)
        {
            return await _context.Actuadores_Cultivo.Include(a => a.Cultivo).FirstOrDefaultAsync(a => a.Id == id && !a.Eliminado);
        }

        public async Task<ServiceResponse> AddActuador(ActuadorCultivoCreateDto actuador, Guid usuarioId)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                var error = await Validar(actuador, null);
                if (error != null)
                {
                    response.Result = ServiceResponseType.Failed;
                    response.ErrorMessage = error;
                    return response;
                }

                var nuevo = new ActuadorCultivo
                {
                    Id = Guid.NewGuid(),
                    CultivoId = actuador.CultivoId,
                    Nombre = actuador.Nombre.Trim(),
                    Clave = actuador.Clave.Trim(),
                    DeviceId = actuador.DeviceId,
                    Tipo = actuador.Tipo.Trim(),
                    Marca = actuador.Marca,
                    Potencia = actuador.Potencia,
                    Especificaciones = actuador.Especificaciones,
                    Modo = ModosActuador.Automatico,
                    Encendido = false,
                    Activo = true,
                    FechaUltimoCambio = DateTime.Now
                };

                _context.Actuadores_Cultivo.Add(nuevo);
                _bitacora.AgregarAuditoria(nuevo.CultivoId, "Actuador", nuevo.Id, nuevo.Nombre, AccionesAuditoria.Creado,
                    $"{nuevo.Tipo} · clave '{nuevo.Clave}' · modo automático", actuador.Motivo, usuarioId);
                await _context.SaveChangesAsync();

                response.Result = ServiceResponseType.Succeded;
                response.Data = nuevo.Id;
            }
            catch (Exception ex)
            {
                response.Result = ServiceResponseType.Failed;
                response.ErrorMessage = ex.Message;
            }
            return response;
        }

        public async Task<ServiceResponse> UpdateActuador(Guid id, ActuadorCultivoCreateDto actuador, Guid usuarioId)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                var existente = await _context.Actuadores_Cultivo.FirstOrDefaultAsync(a => a.Id == id && !a.Eliminado);
                if (existente == null)
                {
                    response.Result = ServiceResponseType.Failed;
                    response.ErrorMessage = "El actuador no existe";
                    return response;
                }

                var error = await Validar(actuador, id);
                if (error != null)
                {
                    response.Result = ServiceResponseType.Failed;
                    response.ErrorMessage = error;
                    return response;
                }

                var detalle = BitacoraCultivoService.DescribirCambios(
                    ("Nombre", existente.Nombre, actuador.Nombre.Trim()),
                    ("Clave", existente.Clave, actuador.Clave.Trim()),
                    ("Tipo", existente.Tipo, actuador.Tipo.Trim()),
                    ("Marca", existente.Marca, actuador.Marca),
                    ("Potencia", existente.Potencia, actuador.Potencia),
                    ("Especificaciones", existente.Especificaciones, actuador.Especificaciones));

                existente.Nombre = actuador.Nombre.Trim();
                existente.Clave = actuador.Clave.Trim();
                existente.DeviceId = actuador.DeviceId;
                existente.Tipo = actuador.Tipo.Trim();
                existente.Marca = actuador.Marca;
                existente.Potencia = actuador.Potencia;
                existente.Especificaciones = actuador.Especificaciones;

                if (detalle != null)
                {
                    _bitacora.AgregarAuditoria(existente.CultivoId, "Actuador", existente.Id, existente.Nombre,
                        AccionesAuditoria.Modificado, detalle, actuador.Motivo, usuarioId);
                }
                await _context.SaveChangesAsync();

                response.Result = ServiceResponseType.Succeded;
            }
            catch (Exception ex)
            {
                response.Result = ServiceResponseType.Failed;
                response.ErrorMessage = ex.Message;
            }
            return response;
        }

        // Eliminación lógica. Si estaba encendido se manda a apagar.
        public async Task<ServiceResponse> DeleteActuador(Guid id, string motivo, Guid usuarioId)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                if (string.IsNullOrWhiteSpace(motivo))
                {
                    response.Result = ServiceResponseType.Failed;
                    response.ErrorMessage = "El motivo es obligatorio para eliminar un actuador";
                    return response;
                }

                var existente = await _context.Actuadores_Cultivo.Include(a => a.Cultivo).FirstOrDefaultAsync(a => a.Id == id && !a.Eliminado);
                if (existente == null)
                {
                    response.Result = ServiceResponseType.Failed;
                    response.ErrorMessage = "El actuador no existe";
                    return response;
                }

                var umbrales = await _context.Umbrales_Cultivo.CountAsync(u => u.ActuadorCultivoId == id && !u.Eliminado);
                if (umbrales > 0)
                {
                    response.Result = ServiceResponseType.Failed;
                    response.ErrorMessage = $"El actuador está asociado a {umbrales} umbral(es). Quítelo de esos umbrales primero";
                    return response;
                }

                var estabaEncendido = existente.Encendido;
                existente.Eliminado = true;
                existente.Activo = false;
                existente.Encendido = false;
                existente.FechaUltimoCambio = DateTime.Now;

                if (estabaEncendido)
                {
                    _bitacora.AgregarEvento(existente.CultivoId, "Actuador", existente.Id, existente.Nombre,
                        "Apagado", "Manual", "Apagado por eliminación del actuador", usuarioId);
                }
                _bitacora.AgregarAuditoria(existente.CultivoId, "Actuador", existente.Id, existente.Nombre,
                    AccionesAuditoria.Eliminado, null, motivo, usuarioId);
                await _context.SaveChangesAsync();

                if (estabaEncendido) await EnviarComando(existente, "Manual");

                response.Result = ServiceResponseType.Succeded;
            }
            catch (Exception ex)
            {
                response.Result = ServiceResponseType.Failed;
                response.ErrorMessage = ex.Message;
            }
            return response;
        }

        // Encendido/apagado manual. Pasa el actuador a modo Manual para que los umbrales no
        // reviertan lo que hizo la persona; para devolverlo al sistema se usa CambiarModo.
        public async Task<ServiceResponse> Accionar(Guid id, AccionarActuadorDto accion, Guid usuarioId)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                if (string.IsNullOrWhiteSpace(accion.Motivo))
                {
                    response.Result = ServiceResponseType.Failed;
                    response.ErrorMessage = "Indique para qué enciende o apaga el actuador";
                    return response;
                }

                var actuador = await _context.Actuadores_Cultivo.Include(a => a.Cultivo).FirstOrDefaultAsync(a => a.Id == id && !a.Eliminado);
                if (actuador == null)
                {
                    response.Result = ServiceResponseType.Failed;
                    response.ErrorMessage = "El actuador no existe";
                    return response;
                }

                if (!actuador.Activo)
                {
                    response.Result = ServiceResponseType.Failed;
                    response.ErrorMessage = "El actuador está desactivado";
                    return response;
                }

                if (accion.AlarmaCultivoId.HasValue &&
                    !await _context.Alarmas_Cultivo.AnyAsync(a => a.Id == accion.AlarmaCultivoId && a.CultivoId == actuador.CultivoId))
                {
                    response.Result = ServiceResponseType.Failed;
                    response.ErrorMessage = "La alarma indicada no pertenece a este cultivo";
                    return response;
                }

                var motivo = accion.Motivo.Trim();

                if (actuador.Modo != ModosActuador.Manual)
                {
                    actuador.Modo = ModosActuador.Manual;
                    _bitacora.AgregarEvento(actuador.CultivoId, "Actuador", actuador.Id, actuador.Nombre,
                        "ModoManual", "Manual", "Paso a manual por accionamiento del usuario", usuarioId, accion.AlarmaCultivoId);
                }

                actuador.Encendido = accion.Encender;
                actuador.FechaUltimoCambio = DateTime.Now;

                var nombreAccion = accion.Encender ? "Encendido" : "Apagado";
                _bitacora.AgregarEvento(actuador.CultivoId, "Actuador", actuador.Id, actuador.Nombre,
                    nombreAccion, "Manual", motivo, usuarioId, accion.AlarmaCultivoId);

                if (accion.AlarmaCultivoId.HasValue)
                {
                    _bitacora.AgregarBitacoraAlarma(accion.AlarmaCultivoId.Value, "AccionManual",
                        $"{nombreAccion} {actuador.Nombre}: {motivo}", usuarioId);
                }

                await _context.SaveChangesAsync();
                await EnviarComando(actuador, "Manual");

                response.Result = ServiceResponseType.Succeded;
            }
            catch (Exception ex)
            {
                response.Result = ServiceResponseType.Failed;
                response.ErrorMessage = ex.Message;
            }
            return response;
        }

        public async Task<ServiceResponse> CambiarModo(Guid id, CambiarModoActuadorDto cambio, Guid usuarioId)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                if (cambio.Modo != ModosActuador.Automatico && cambio.Modo != ModosActuador.Manual)
                {
                    response.Result = ServiceResponseType.Failed;
                    response.ErrorMessage = "El modo debe ser Automatico o Manual";
                    return response;
                }

                if (string.IsNullOrWhiteSpace(cambio.Motivo))
                {
                    response.Result = ServiceResponseType.Failed;
                    response.ErrorMessage = "El motivo es obligatorio para cambiar el modo";
                    return response;
                }

                var actuador = await _context.Actuadores_Cultivo.FirstOrDefaultAsync(a => a.Id == id && !a.Eliminado);
                if (actuador == null)
                {
                    response.Result = ServiceResponseType.Failed;
                    response.ErrorMessage = "El actuador no existe";
                    return response;
                }

                if (actuador.Modo != cambio.Modo)
                {
                    actuador.Modo = cambio.Modo;
                    actuador.FechaUltimoCambio = DateTime.Now;
                    _bitacora.AgregarEvento(actuador.CultivoId, "Actuador", actuador.Id, actuador.Nombre,
                        cambio.Modo == ModosActuador.Automatico ? "ModoAutomatico" : "ModoManual", "Manual", cambio.Motivo.Trim(), usuarioId);
                    await _context.SaveChangesAsync();
                    await NotificarFrontend(actuador);
                }

                response.Result = ServiceResponseType.Succeded;
            }
            catch (Exception ex)
            {
                response.Result = ServiceResponseType.Failed;
                response.ErrorMessage = ex.Message;
            }
            return response;
        }

        // Activar/desactivar el actuador en el sistema. Al desactivarlo se manda a apagar.
        public async Task<ServiceResponse> CambiarEstado(Guid id, CambiarEstadoDto cambio, Guid usuarioId)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                if (string.IsNullOrWhiteSpace(cambio.Motivo))
                {
                    response.Result = ServiceResponseType.Failed;
                    response.ErrorMessage = "El motivo es obligatorio para activar o desactivar un actuador";
                    return response;
                }

                var actuador = await _context.Actuadores_Cultivo.Include(a => a.Cultivo).FirstOrDefaultAsync(a => a.Id == id && !a.Eliminado);
                if (actuador == null)
                {
                    response.Result = ServiceResponseType.Failed;
                    response.ErrorMessage = "El actuador no existe";
                    return response;
                }

                if (actuador.Activo != cambio.Activo)
                {
                    var estabaEncendido = actuador.Encendido;
                    actuador.Activo = cambio.Activo;
                    actuador.FechaUltimoCambio = DateTime.Now;
                    _bitacora.AgregarAuditoria(actuador.CultivoId, "Actuador", actuador.Id, actuador.Nombre,
                        cambio.Activo ? AccionesAuditoria.Activado : AccionesAuditoria.Desactivado, null, cambio.Motivo, usuarioId);

                    if (!cambio.Activo && estabaEncendido)
                    {
                        actuador.Encendido = false;
                        _bitacora.AgregarEvento(actuador.CultivoId, "Actuador", actuador.Id, actuador.Nombre,
                            "Apagado", "Manual", "Apagado por desactivación del actuador", usuarioId);
                    }

                    await _context.SaveChangesAsync();

                    if (!cambio.Activo && estabaEncendido) await EnviarComando(actuador, "Manual");
                    else await NotificarFrontend(actuador);
                }

                response.Result = ServiceResponseType.Succeded;
            }
            catch (Exception ex)
            {
                response.Result = ServiceResponseType.Failed;
                response.ErrorMessage = ex.Message;
            }
            return response;
        }

        // El worker escucha este evento y lo publica en cultivo/{Numero}/actuadores/{Clave}/cmd.
        // Si el actuador es de Nueva Naturaleza, se envía por el hub de acuaponía y Worker.cs lo escribe al puerto serial.
        private async Task EnviarComando(ActuadorCultivo actuador, string origen)
        {
            if (actuador.DeviceId.HasValue)
            {
                var nombreDispositivo = await _context.Devices.Where(d => d.Id == actuador.DeviceId)
                    .Select(d => d.Device_Name).FirstOrDefaultAsync();
                if (nombreDispositivo != null)
                {
                    await _actuatorHub.Clients.All.SendAsync("ReceiveCommand", nombreDispositivo, actuador.Encendido ? "Activo" : "Inactivo");
                }
                await NotificarFrontend(actuador);
                return;
            }

            await _hub.Clients.All.SendAsync(CultivoHub.EventoComandoActuador, new
            {
                actuadorId = actuador.Id,
                cultivoNumero = actuador.Cultivo?.Numero ?? 0,
                clave = actuador.Clave,
                accion = actuador.Encendido ? "ON" : "OFF",
                origen
            });
            await NotificarFrontend(actuador);
        }

        private async Task NotificarFrontend(ActuadorCultivo actuador)
        {
            await _hub.Clients.All.SendAsync(CultivoHub.EventoActuador, new
            {
                actuadorId = actuador.Id,
                cultivoId = actuador.CultivoId,
                encendido = actuador.Encendido,
                modo = actuador.Modo,
                activo = actuador.Activo
            });
        }

        private async Task<string?> Validar(ActuadorCultivoCreateDto actuador, Guid? idActual)
        {
            if (string.IsNullOrWhiteSpace(actuador.Nombre)) return "El nombre del actuador es obligatorio";
            if (string.IsNullOrWhiteSpace(actuador.Clave)) return "La clave del actuador es obligatoria (se usa en el topic MQTT)";
            if (actuador.Clave.Contains('/') || actuador.Clave.Contains('+') || actuador.Clave.Contains('#') || actuador.Clave.Contains(' '))
                return "La clave no puede tener espacios ni los caracteres / + #";

            var cultivoId = idActual.HasValue
                ? (await _context.Actuadores_Cultivo.Where(a => a.Id == idActual).Select(a => a.CultivoId).FirstAsync())
                : actuador.CultivoId;

            if (!idActual.HasValue && !await _context.Cultivos.AnyAsync(c => c.Id == cultivoId && !c.Eliminado))
                return "El cultivo no existe";

            var claveRepetida = await _context.Actuadores_Cultivo
                .AnyAsync(a => a.CultivoId == cultivoId && a.Clave == actuador.Clave.Trim() && a.Id != idActual && !a.Eliminado);
            if (claveRepetida) return $"Ya existe un actuador con la clave '{actuador.Clave}' en este cultivo";

            if (actuador.DeviceId.HasValue)
            {
                var esActuador = await _context.Devices.AnyAsync(d => d.Id == actuador.DeviceId && d.Device_Type!.Device_Type_Name == "Actuador");
                if (!esActuador) return "El dispositivo de Nueva Naturaleza no existe o no es un actuador";

                var yaVinculado = await _context.Actuadores_Cultivo
                    .AnyAsync(a => a.CultivoId == cultivoId && a.DeviceId == actuador.DeviceId && a.Id != idActual && !a.Eliminado);
                if (yaVinculado) return "Ese actuador de Nueva Naturaleza ya está vinculado a este cultivo";
            }

            return null;
        }
    }
}
