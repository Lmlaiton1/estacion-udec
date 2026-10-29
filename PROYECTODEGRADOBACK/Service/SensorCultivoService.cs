using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RR_Nueva_Naturaleza.DAL;
using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Models.Dto;

namespace RR_Nueva_Naturaleza.Service
{
    public class SensorCultivoService : ISensorCultivoService
    {
        private readonly RR_Nueva_NaturalezaContext _context;
        private readonly IBitacoraCultivoService _bitacora;
        private readonly IMapper _mapper;

        public SensorCultivoService(RR_Nueva_NaturalezaContext context, IBitacoraCultivoService bitacora, IMapper mapper)
        {
            _context = context;
            _bitacora = bitacora;
            _mapper = mapper;
        }

        public async Task<IEnumerable<SensorCultivoDto>> GetSensores(Guid? cultivoId)
        {
            var query = _context.Sensores_Cultivo.Include(s => s.Cultivo).Where(s => !s.Eliminado);
            if (cultivoId.HasValue) query = query.Where(s => s.CultivoId == cultivoId.Value);

            var sensores = await query.OrderBy(s => s.Nombre).ToListAsync();
            var dtos = _mapper.Map<List<SensorCultivo>, List<SensorCultivoDto>>(sensores);

            // Último valor de cada sensor para mostrarlo en la tabla
            var ids = sensores.Select(s => s.Id).ToList();
            var ultimas = await _context.Lecturas_Cultivo
                .Where(l => ids.Contains(l.SensorCultivoId))
                .GroupBy(l => l.SensorCultivoId)
                .Select(g => g.OrderByDescending(l => l.FechaHora).First())
                .ToListAsync();

            foreach (var dto in dtos)
            {
                var ultima = ultimas.FirstOrDefault(l => l.SensorCultivoId == dto.Id);
                dto.UltimoValor = ultima?.Valor;
                dto.FechaUltimoValor = ultima?.FechaHora;
            }

            return dtos;
        }

        public async Task<SensorCultivo?> GetSensor(Guid id)
        {
            return await _context.Sensores_Cultivo.Include(s => s.Cultivo).FirstOrDefaultAsync(s => s.Id == id && !s.Eliminado);
        }

        public async Task<ServiceResponse> AddSensor(SensorCultivoCreateDto sensor, Guid usuarioId)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                var error = await Validar(sensor, null);
                if (error != null) return Fallo(response, error);

                var nuevo = new SensorCultivo
                {
                    Id = Guid.NewGuid(),
                    CultivoId = sensor.CultivoId,
                    Nombre = sensor.Nombre.Trim(),
                    Clave = sensor.Clave.Trim(),
                    DeviceId = sensor.DeviceId,
                    Tipo = sensor.Tipo.Trim(),
                    Variable = sensor.Variable.Trim(),
                    Unidad = sensor.Unidad.Trim(),
                    RangoMin = sensor.RangoMin,
                    RangoMax = sensor.RangoMax,
                    Precision = sensor.Precision,
                    Marca = sensor.Marca,
                    Especificaciones = sensor.Especificaciones,
                    Activo = true,
                    FechaInstalacion = sensor.FechaInstalacion ?? DateTime.Now
                };

                _context.Sensores_Cultivo.Add(nuevo);
                _bitacora.AgregarAuditoria(nuevo.CultivoId, "Sensor", nuevo.Id, nuevo.Nombre, AccionesAuditoria.Creado,
                    $"{nuevo.Tipo} · {nuevo.Variable} ({nuevo.Unidad}) · clave '{nuevo.Clave}'", sensor.Motivo, usuarioId);
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

        public async Task<ServiceResponse> UpdateSensor(Guid id, SensorCultivoCreateDto sensor, Guid usuarioId)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                var existente = await _context.Sensores_Cultivo.FirstOrDefaultAsync(s => s.Id == id && !s.Eliminado);
                if (existente == null) return Fallo(response, "El sensor no existe");

                var error = await Validar(sensor, id);
                if (error != null) return Fallo(response, error);

                var fechaInstalacion = sensor.FechaInstalacion ?? existente.FechaInstalacion;
                var detalle = BitacoraCultivoService.DescribirCambios(
                    ("Nombre", existente.Nombre, sensor.Nombre.Trim()),
                    ("Clave", existente.Clave, sensor.Clave.Trim()),
                    ("Tipo", existente.Tipo, sensor.Tipo.Trim()),
                    ("Variable", existente.Variable, sensor.Variable.Trim()),
                    ("Unidad", existente.Unidad, sensor.Unidad.Trim()),
                    ("Rango mín.", existente.RangoMin, sensor.RangoMin),
                    ("Rango máx.", existente.RangoMax, sensor.RangoMax),
                    ("Precisión", existente.Precision, sensor.Precision),
                    ("Marca", existente.Marca, sensor.Marca),
                    ("Especificaciones", existente.Especificaciones, sensor.Especificaciones),
                    ("Fecha de instalación", existente.FechaInstalacion, fechaInstalacion));

                // El cultivo no se cambia: las lecturas y umbrales ya están ligados a él
                existente.Nombre = sensor.Nombre.Trim();
                existente.Clave = sensor.Clave.Trim();
                existente.DeviceId = sensor.DeviceId;
                existente.Tipo = sensor.Tipo.Trim();
                existente.Variable = sensor.Variable.Trim();
                existente.Unidad = sensor.Unidad.Trim();
                existente.RangoMin = sensor.RangoMin;
                existente.RangoMax = sensor.RangoMax;
                existente.Precision = sensor.Precision;
                existente.Marca = sensor.Marca;
                existente.Especificaciones = sensor.Especificaciones;
                existente.FechaInstalacion = fechaInstalacion;

                if (detalle != null)
                {
                    _bitacora.AgregarAuditoria(existente.CultivoId, "Sensor", existente.Id, existente.Nombre,
                        AccionesAuditoria.Modificado, detalle, sensor.Motivo, usuarioId);
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

        // Eliminación lógica: las lecturas históricas se conservan
        public async Task<ServiceResponse> DeleteSensor(Guid id, string motivo, Guid usuarioId)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                if (string.IsNullOrWhiteSpace(motivo)) return Fallo(response, "El motivo es obligatorio para eliminar un sensor");

                var existente = await _context.Sensores_Cultivo.FirstOrDefaultAsync(s => s.Id == id && !s.Eliminado);
                if (existente == null) return Fallo(response, "El sensor no existe");

                var umbrales = await _context.Umbrales_Cultivo.CountAsync(u => u.SensorCultivoId == id && !u.Eliminado);
                if (umbrales > 0)
                    return Fallo(response, $"El sensor tiene {umbrales} umbral(es). Elimínelos primero");

                existente.Eliminado = true;
                existente.Activo = false;
                _bitacora.AgregarAuditoria(existente.CultivoId, "Sensor", existente.Id, existente.Nombre,
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

        public async Task<ServiceResponse> CambiarEstado(Guid id, CambiarEstadoDto cambio, Guid usuarioId)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                if (string.IsNullOrWhiteSpace(cambio.Motivo))
                    return Fallo(response, "El motivo es obligatorio para activar o desactivar un sensor");

                var existente = await _context.Sensores_Cultivo.FirstOrDefaultAsync(s => s.Id == id && !s.Eliminado);
                if (existente == null) return Fallo(response, "El sensor no existe");

                if (existente.Activo != cambio.Activo)
                {
                    existente.Activo = cambio.Activo;
                    _bitacora.AgregarAuditoria(existente.CultivoId, "Sensor", existente.Id, existente.Nombre,
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

        public async Task<IEnumerable<LecturaCultivo>> GetLecturas(Guid sensorId, int horas)
        {
            var desde = DateTime.Now.AddHours(-Math.Clamp(horas, 1, 24 * 31));
            return await _context.Lecturas_Cultivo
                .Where(l => l.SensorCultivoId == sensorId && l.FechaHora >= desde)
                .OrderBy(l => l.FechaHora)
                .ToListAsync();
        }

        private async Task<string?> Validar(SensorCultivoCreateDto sensor, Guid? idActual)
        {
            if (string.IsNullOrWhiteSpace(sensor.Nombre)) return "El nombre del sensor es obligatorio";
            if (string.IsNullOrWhiteSpace(sensor.Clave)) return "La clave del sensor es obligatoria (campo del JSON que envía el ESP)";
            if (string.IsNullOrWhiteSpace(sensor.Variable)) return "La variable es obligatoria";
            if (sensor.RangoMin.HasValue && sensor.RangoMax.HasValue && sensor.RangoMin > sensor.RangoMax)
                return "El rango mínimo no puede ser mayor al máximo";

            var cultivoId = idActual.HasValue
                ? (await _context.Sensores_Cultivo.Where(s => s.Id == idActual).Select(s => s.CultivoId).FirstAsync())
                : sensor.CultivoId;

            if (!idActual.HasValue && !await _context.Cultivos.AnyAsync(c => c.Id == cultivoId && !c.Eliminado))
                return "El cultivo no existe";

            var claveRepetida = await _context.Sensores_Cultivo
                .AnyAsync(s => s.CultivoId == cultivoId && s.Clave == sensor.Clave.Trim() && s.Id != idActual && !s.Eliminado);
            if (claveRepetida) return $"Ya existe un sensor con la clave '{sensor.Clave}' en este cultivo";

            if (sensor.DeviceId.HasValue)
            {
                var esSensor = await _context.Devices.AnyAsync(d => d.Id == sensor.DeviceId && d.Device_Type!.Device_Type_Name == "Sensor");
                if (!esSensor) return "El dispositivo de Nueva Naturaleza no existe o no es un sensor";

                var yaVinculado = await _context.Sensores_Cultivo
                    .AnyAsync(s => s.CultivoId == cultivoId && s.DeviceId == sensor.DeviceId && s.Id != idActual && !s.Eliminado);
                if (yaVinculado) return "Ese sensor de Nueva Naturaleza ya está vinculado a este cultivo";
            }

            return null;
        }

        private static ServiceResponse Fallo(ServiceResponse response, string mensaje)
        {
            response.Result = ServiceResponseType.Failed;
            response.ErrorMessage = mensaje;
            return response;
        }
    }
}
