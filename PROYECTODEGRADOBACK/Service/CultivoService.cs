using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RR_Nueva_Naturaleza.DAL;
using RR_Nueva_Naturaleza.Hubs;
using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Models.Dto;

namespace RR_Nueva_Naturaleza.Service
{
    public class CultivoService : ICultivoService
    {
        private readonly RR_Nueva_NaturalezaContext _context;
        private readonly IBitacoraCultivoService _bitacora;
        private readonly IHubContext<CultivoHub> _hub;

        public CultivoService(RR_Nueva_NaturalezaContext context, IBitacoraCultivoService bitacora, IHubContext<CultivoHub> hub)
        {
            _context = context;
            _bitacora = bitacora;
            _hub = hub;
        }

        public async Task<IEnumerable<Cultivo>> GetCultivos()
        {
            return await _context.Cultivos
                .Where(c => !c.Eliminado)
                .Include(c => c.Etapas!.Where(e => !e.Eliminado))
                .Include(c => c.Estaciones)
                .OrderBy(c => c.Numero)
                .ToListAsync();
        }

        public async Task<Cultivo?> GetCultivo(Guid id)
        {
            return await _context.Cultivos
                .Include(c => c.Etapas!.Where(e => !e.Eliminado))
                .Include(c => c.Estaciones)
                .FirstOrDefaultAsync(c => c.Id == id && !c.Eliminado);
        }

        public async Task<ServiceResponse> AddCultivo(CultivoCreateDto cultivo, Guid usuarioId)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                var error = await ValidarCultivo(cultivo, null);
                if (error != null) return Fallo(response, error);

                var nuevo = new Cultivo
                {
                    Id = Guid.NewGuid(),
                    Numero = cultivo.Numero,
                    Nombre = cultivo.Nombre.Trim(),
                    Especie = cultivo.Especie.Trim(),
                    Variedad = cultivo.Variedad,
                    Ubicacion = cultivo.Ubicacion,
                    AreaM2 = cultivo.AreaM2,
                    FechaSiembra = cultivo.FechaSiembra,
                    Descripcion = cultivo.Descripcion,
                    Activo = true,
                    CreadoPor = usuarioId,
                    FechaCreacion = DateTime.Now
                };

                var estaciones = cultivo.Estaciones.Distinct().OrderBy(n => n).ToList();
                nuevo.Estaciones = estaciones.Select(n => new CultivoEstacion { CultivoId = nuevo.Id, EstacionNumero = n }).ToList();

                _context.Cultivos.Add(nuevo);
                _bitacora.AgregarAuditoria(nuevo.Id, "Cultivo", nuevo.Id, NombreCultivo(nuevo), AccionesAuditoria.Creado,
                    $"Especie: {nuevo.Especie}; Estaciones: {await TextoEstaciones(estaciones)}", cultivo.Motivo, usuarioId);
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

        public async Task<ServiceResponse> UpdateCultivo(Guid id, CultivoCreateDto cultivo, Guid usuarioId)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                var existente = await _context.Cultivos.Include(c => c.Estaciones).FirstOrDefaultAsync(c => c.Id == id && !c.Eliminado);
                if (existente == null) return Fallo(response, "El cultivo no existe");

                var error = await ValidarCultivo(cultivo, id);
                if (error != null) return Fallo(response, error);

                var detalle = BitacoraCultivoService.DescribirCambios(
                    ("Número", existente.Numero, cultivo.Numero),
                    ("Nombre", existente.Nombre, cultivo.Nombre.Trim()),
                    ("Especie", existente.Especie, cultivo.Especie.Trim()),
                    ("Variedad", existente.Variedad, cultivo.Variedad),
                    ("Ubicación", existente.Ubicacion, cultivo.Ubicacion),
                    ("Área m²", existente.AreaM2, cultivo.AreaM2),
                    ("Fecha de siembra", existente.FechaSiembra, cultivo.FechaSiembra),
                    ("Estaciones", await TextoEstaciones(existente.Estaciones!.Select(e => e.EstacionNumero)),
                                   await TextoEstaciones(cultivo.Estaciones)),
                    ("Descripción", existente.Descripcion, cultivo.Descripcion));

                existente.Numero = cultivo.Numero;
                existente.Nombre = cultivo.Nombre.Trim();
                existente.Especie = cultivo.Especie.Trim();
                existente.Variedad = cultivo.Variedad;
                existente.Ubicacion = cultivo.Ubicacion;
                existente.AreaM2 = cultivo.AreaM2;
                existente.FechaSiembra = cultivo.FechaSiembra;
                SincronizarEstaciones(existente, cultivo.Estaciones);
                existente.Descripcion = cultivo.Descripcion;

                if (detalle != null)
                {
                    _bitacora.AgregarAuditoria(existente.Id, "Cultivo", existente.Id, NombreCultivo(existente),
                        AccionesAuditoria.Modificado, detalle, cultivo.Motivo, usuarioId);
                }
                await _context.SaveChangesAsync();

                response.Result = ServiceResponseType.Succeded;
                response.Data = existente.Id;
            }
            catch (Exception ex)
            {
                response.Result = ServiceResponseType.Failed;
                response.ErrorMessage = ex.Message;
            }
            return response;
        }

        // Un cultivo inactivo no procesa lecturas ni evalúa umbrales (el worker lo ignora)
        public async Task<ServiceResponse> CambiarEstado(Guid id, CambiarEstadoDto cambio, Guid usuarioId)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                if (string.IsNullOrWhiteSpace(cambio.Motivo))
                    return Fallo(response, "El motivo es obligatorio para activar o desactivar un cultivo");

                var existente = await _context.Cultivos.FirstOrDefaultAsync(c => c.Id == id && !c.Eliminado);
                if (existente == null) return Fallo(response, "El cultivo no existe");

                if (existente.Activo != cambio.Activo)
                {
                    existente.Activo = cambio.Activo;
                    _bitacora.AgregarAuditoria(existente.Id, "Cultivo", existente.Id, NombreCultivo(existente),
                        cambio.Activo ? AccionesAuditoria.Activado : AccionesAuditoria.Desactivado, null, cambio.Motivo, usuarioId);
                    await _context.SaveChangesAsync();
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

        // Eliminación lógica en cascada: etapas, sensores, actuadores y umbrales del cultivo quedan
        // eliminados; las alarmas abiertas se anulan y los actuadores encendidos se mandan a apagar.
        public async Task<ServiceResponse> DeleteCultivo(Guid id, string motivo, Guid usuarioId)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                if (string.IsNullOrWhiteSpace(motivo)) return Fallo(response, "El motivo es obligatorio para eliminar un cultivo");

                var cultivo = await _context.Cultivos.FirstOrDefaultAsync(c => c.Id == id && !c.Eliminado);
                if (cultivo == null) return Fallo(response, "El cultivo no existe");

                var etapas = await _context.Etapas_Cultivo.Where(e => e.CultivoId == id && !e.Eliminado).ToListAsync();
                var sensores = await _context.Sensores_Cultivo.Where(s => s.CultivoId == id && !s.Eliminado).ToListAsync();
                var actuadores = await _context.Actuadores_Cultivo.Where(a => a.CultivoId == id && !a.Eliminado).ToListAsync();
                var umbrales = await _context.Umbrales_Cultivo.Where(u => u.CultivoId == id && !u.Eliminado).ToListAsync();
                var alarmasAbiertas = await _context.Alarmas_Cultivo
                    .Where(a => a.CultivoId == id && !a.Eliminado && a.Estado != EstadosAlarma.Resuelta && a.Estado != EstadosAlarma.Anulada)
                    .ToListAsync();

                cultivo.Eliminado = true;
                cultivo.Activo = false;
                etapas.ForEach(e => { e.Eliminado = true; e.Activa = false; });
                sensores.ForEach(s => { s.Eliminado = true; s.Activo = false; });
                umbrales.ForEach(u => { u.Eliminado = true; u.Activo = false; });

                var encendidos = actuadores.Where(a => a.Encendido).ToList();
                foreach (var a in actuadores)
                {
                    a.Eliminado = true;
                    a.Activo = false;
                    if (a.Encendido)
                    {
                        a.Encendido = false;
                        a.FechaUltimoCambio = DateTime.Now;
                        _bitacora.AgregarEvento(id, "Actuador", a.Id, a.Nombre, "Apagado", "Manual",
                            "Apagado por eliminación del cultivo", usuarioId);
                    }
                }

                foreach (var alarma in alarmasAbiertas)
                {
                    alarma.Estado = EstadosAlarma.Anulada;
                    _bitacora.AgregarBitacoraAlarma(alarma.Id, "Anulada", $"Cultivo eliminado: {motivo.Trim()}", usuarioId);
                }

                var detalle = $"Incluye {etapas.Count} etapas, {sensores.Count} sensores, {actuadores.Count} actuadores, " +
                              $"{umbrales.Count} umbrales; {alarmasAbiertas.Count} alarmas abiertas anuladas";
                _bitacora.AgregarAuditoria(id, "Cultivo", id, NombreCultivo(cultivo), AccionesAuditoria.Eliminado, detalle, motivo, usuarioId);

                await _context.SaveChangesAsync();

                foreach (var a in encendidos)
                {
                    await _hub.Clients.All.SendAsync(CultivoHub.EventoComandoActuador, new
                    {
                        actuadorId = a.Id,
                        cultivoNumero = cultivo.Numero,
                        clave = a.Clave,
                        accion = "OFF",
                        origen = "Manual"
                    });
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

        public async Task<IEnumerable<EtapaCultivo>> GetEtapas(Guid cultivoId)
        {
            return await _context.Etapas_Cultivo
                .Where(e => e.CultivoId == cultivoId && !e.Eliminado)
                .OrderBy(e => e.Orden)
                .ToListAsync();
        }

        public async Task<ServiceResponse> AddEtapa(EtapaCultivoCreateDto etapa, Guid usuarioId)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                if (string.IsNullOrWhiteSpace(etapa.Nombre)) return Fallo(response, "El nombre de la etapa es obligatorio");

                var cultivo = await _context.Cultivos.FirstOrDefaultAsync(c => c.Id == etapa.CultivoId && !c.Eliminado);
                if (cultivo == null) return Fallo(response, "El cultivo no existe");

                // La primera etapa que se registra queda como etapa actual
                var esPrimera = !await _context.Etapas_Cultivo.AnyAsync(e => e.CultivoId == etapa.CultivoId && !e.Eliminado);

                var nueva = new EtapaCultivo
                {
                    Id = Guid.NewGuid(),
                    CultivoId = etapa.CultivoId,
                    Nombre = etapa.Nombre.Trim(),
                    Orden = etapa.Orden,
                    DuracionDias = etapa.DuracionDias,
                    FechaInicio = etapa.FechaInicio,
                    Descripcion = etapa.Descripcion,
                    Activa = esPrimera
                };

                _context.Etapas_Cultivo.Add(nueva);
                _bitacora.AgregarAuditoria(cultivo.Id, "Etapa", nueva.Id, $"{nueva.Nombre} ({cultivo.Nombre})", AccionesAuditoria.Creado,
                    esPrimera ? "Queda como etapa actual por ser la primera" : $"Orden {nueva.Orden}", etapa.Motivo, usuarioId);
                await _context.SaveChangesAsync();

                response.Result = ServiceResponseType.Succeded;
                response.Data = nueva.Id;
            }
            catch (Exception ex)
            {
                response.Result = ServiceResponseType.Failed;
                response.ErrorMessage = ex.Message;
            }
            return response;
        }

        public async Task<ServiceResponse> UpdateEtapa(Guid id, EtapaCultivoCreateDto etapa, Guid usuarioId)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                var existente = await _context.Etapas_Cultivo.Include(e => e.Cultivo).FirstOrDefaultAsync(e => e.Id == id && !e.Eliminado);
                if (existente == null) return Fallo(response, "La etapa no existe");
                if (string.IsNullOrWhiteSpace(etapa.Nombre)) return Fallo(response, "El nombre de la etapa es obligatorio");

                var detalle = BitacoraCultivoService.DescribirCambios(
                    ("Nombre", existente.Nombre, etapa.Nombre.Trim()),
                    ("Orden", existente.Orden, etapa.Orden),
                    ("Duración (días)", existente.DuracionDias, etapa.DuracionDias),
                    ("Fecha de inicio", existente.FechaInicio, etapa.FechaInicio),
                    ("Descripción", existente.Descripcion, etapa.Descripcion));

                existente.Nombre = etapa.Nombre.Trim();
                existente.Orden = etapa.Orden;
                existente.DuracionDias = etapa.DuracionDias;
                existente.FechaInicio = etapa.FechaInicio;
                existente.Descripcion = etapa.Descripcion;

                if (detalle != null)
                {
                    _bitacora.AgregarAuditoria(existente.CultivoId, "Etapa", existente.Id, NombreEtapa(existente),
                        AccionesAuditoria.Modificado, detalle, etapa.Motivo, usuarioId);
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

        public async Task<ServiceResponse> DeleteEtapa(Guid id, string motivo, Guid usuarioId)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                if (string.IsNullOrWhiteSpace(motivo)) return Fallo(response, "El motivo es obligatorio para eliminar una etapa");

                var existente = await _context.Etapas_Cultivo.Include(e => e.Cultivo).FirstOrDefaultAsync(e => e.Id == id && !e.Eliminado);
                if (existente == null) return Fallo(response, "La etapa no existe");

                var umbrales = await _context.Umbrales_Cultivo.CountAsync(u => u.EtapaCultivoId == id && !u.Eliminado);
                if (umbrales > 0)
                    return Fallo(response, $"La etapa tiene {umbrales} umbral(es). Elimínelos o cámbielos de etapa primero");

                existente.Eliminado = true;
                existente.Activa = false;
                _bitacora.AgregarAuditoria(existente.CultivoId, "Etapa", existente.Id, NombreEtapa(existente),
                    AccionesAuditoria.Eliminado, null, motivo, usuarioId);
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

        // Deja esta etapa como la actual del cultivo. Los umbrales de esa etapa pasan a evaluarse.
        public async Task<ServiceResponse> ActivarEtapa(Guid id, string motivo, Guid usuarioId)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                if (string.IsNullOrWhiteSpace(motivo)) return Fallo(response, "El motivo es obligatorio para cambiar de etapa");

                var etapa = await _context.Etapas_Cultivo.Include(e => e.Cultivo).FirstOrDefaultAsync(e => e.Id == id && !e.Eliminado);
                if (etapa == null) return Fallo(response, "La etapa no existe");

                var etapasDelCultivo = await _context.Etapas_Cultivo.Where(e => e.CultivoId == etapa.CultivoId && !e.Eliminado).ToListAsync();
                var anterior = etapasDelCultivo.FirstOrDefault(e => e.Activa && e.Id != id);
                foreach (var e in etapasDelCultivo)
                {
                    e.Activa = e.Id == id;
                }
                etapa.FechaInicio ??= DateTime.Now;

                _bitacora.AgregarAuditoria(etapa.CultivoId, "Etapa", etapa.Id, NombreEtapa(etapa), AccionesAuditoria.EtapaActual,
                    $"Etapa actual: {anterior?.Nombre ?? "(ninguna)"} → {etapa.Nombre}", motivo, usuarioId);
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

        // El cultivo queda sin etapa actual: solo se evalúan los umbrales de "todas las etapas"
        public async Task<ServiceResponse> QuitarEtapaActual(Guid id, string motivo, Guid usuarioId)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                if (string.IsNullOrWhiteSpace(motivo)) return Fallo(response, "El motivo es obligatorio");

                var etapa = await _context.Etapas_Cultivo.Include(e => e.Cultivo).FirstOrDefaultAsync(e => e.Id == id && !e.Eliminado);
                if (etapa == null) return Fallo(response, "La etapa no existe");
                if (!etapa.Activa) return Fallo(response, "La etapa no es la etapa actual");

                etapa.Activa = false;
                _bitacora.AgregarAuditoria(etapa.CultivoId, "Etapa", etapa.Id, NombreEtapa(etapa), AccionesAuditoria.QuitarEtapaActual,
                    "El cultivo queda sin etapa actual", motivo, usuarioId);
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

        // Deja en el cultivo exactamente las estaciones pedidas (agrega las nuevas, quita las que sobran)
        private static void SincronizarEstaciones(Cultivo cultivo, IEnumerable<int> pedidas)
        {
            var nuevas = pedidas.Distinct().ToHashSet();
            var actuales = cultivo.Estaciones!.ToList();

            foreach (var e in actuales.Where(e => !nuevas.Contains(e.EstacionNumero)))
                cultivo.Estaciones!.Remove(e);

            foreach (var n in nuevas.Where(n => actuales.All(e => e.EstacionNumero != n)))
                cultivo.Estaciones!.Add(new CultivoEstacion { CultivoId = cultivo.Id, EstacionNumero = n });
        }

        // "Todas (1, 2, 3)" cuando incluye todas las estaciones registradas; "(ninguna)" si está vacía
        private async Task<string> TextoEstaciones(IEnumerable<int> numeros)
        {
            var lista = numeros.Distinct().OrderBy(n => n).ToList();
            if (lista.Count == 0) return "(ninguna)";

            var todas = await _context.Estaciones_Meteo.Select(e => e.Numero).OrderBy(n => n).ToListAsync();
            var texto = string.Join(", ", lista);
            return lista.SequenceEqual(todas) ? $"Todas ({texto})" : texto;
        }

        private async Task<string?> ValidarCultivo(CultivoCreateDto cultivo, Guid? idActual)
        {
            var estaciones = cultivo.Estaciones.Distinct().ToList();
            if (estaciones.Count > 0)
            {
                var existentes = await _context.Estaciones_Meteo.Where(e => estaciones.Contains(e.Numero)).Select(e => e.Numero).ToListAsync();
                var faltantes = estaciones.Except(existentes).ToList();
                if (faltantes.Count > 0) return $"No existe la estación {string.Join(", ", faltantes)}";
            }

            if (cultivo.Numero <= 0) return "El número del cultivo debe ser mayor a cero";
            if (string.IsNullOrWhiteSpace(cultivo.Nombre)) return "El nombre del cultivo es obligatorio";
            if (string.IsNullOrWhiteSpace(cultivo.Especie)) return "La especie del cultivo es obligatoria";

            var numeroRepetido = await _context.Cultivos.AnyAsync(c => c.Numero == cultivo.Numero && c.Id != idActual && !c.Eliminado);
            if (numeroRepetido) return $"Ya existe un cultivo con el número {cultivo.Numero}";

            return null;
        }

        private static string NombreCultivo(Cultivo c) => $"{c.Numero} · {c.Nombre}";

        private static string NombreEtapa(EtapaCultivo e) => e.Cultivo != null ? $"{e.Nombre} ({e.Cultivo.Nombre})" : e.Nombre;

        private static ServiceResponse Fallo(ServiceResponse response, string mensaje)
        {
            response.Result = ServiceResponseType.Failed;
            response.ErrorMessage = mensaje;
            return response;
        }
    
        public async Task<IEnumerable<DispositivoNuevaNaturalezaDto>> GetDispositivosNuevaNaturaleza()
        {
            var dispositivos = await _context.Devices
                .Where(d => d.Device_Type!.Device_Type_Name == "Sensor" || d.Device_Type!.Device_Type_Name == "Actuador")
                .Select(d => new DispositivoNuevaNaturalezaDto
                {
                    Id = d.Id,
                    Nombre = d.Device_Name,
                    Tipo = d.Device_Type!.Device_Type_Name,
                    Sistema = d.System_Type != null ? d.System_Type.System_Type_Name : string.Empty,
                    Estado = d.Device_Status != null ? d.Device_Status.State_Name : string.Empty,
                    ModoAutomaticoAcuaponia = _context.ActuatorModes.Any(m => m.DeviceId == d.Id && m.IsAutoMode)
                })
                .OrderBy(d => d.Tipo).ThenBy(d => d.Nombre)
                .ToListAsync();

            // Última medición de cada sensor (consulta aparte por sensor: Measurements es grande y está indexada por DeviceId)
            foreach (var sensor in dispositivos.Where(d => d.Tipo == "Sensor"))
            {
                var ultima = await _context.Measurements.Where(m => m.DeviceId == sensor.Id)
                    .OrderByDescending(m => m.Date)
                    .Select(m => new { m.Value, m.Date })
                    .FirstOrDefaultAsync();
                sensor.UltimoValor = ultima?.Value;
                sensor.FechaUltimoValor = ultima?.Date;
            }

            return dispositivos;
        }
    }
}
