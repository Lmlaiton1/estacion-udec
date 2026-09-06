using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RR_Nueva_Naturaleza.DAL;
using RR_Nueva_Naturaleza.Hubs;
using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Models.Dto;
using RR_Nueva_Naturaleza.Models.Request;
using System.IO.Ports;


namespace RR_Nueva_Naturaleza.Service
{
    public class DeviceService : IDeviceService
    {
        private readonly RR_Nueva_NaturalezaContext _context;
        private readonly IMapper _mapper;
        private readonly IHubContext<ActuatorHub> _hubContext;

        public DeviceService(RR_Nueva_NaturalezaContext context, IMapper mapper, IHubContext<ActuatorHub> hubContex) //Constructor con inyeccion de dependencias
        {
            _context = context;
            _mapper = mapper;
            _hubContext = hubContex;
        }

        public async Task<ServiceResponse> AddSensor(SensorCreatePostDto sensor)
        {
            try
            {
                var type = await _context.Device_Types.FirstOrDefaultAsync(x => x.Device_Type_Name == "Sensor");
                var state = await _context.Device_States.FirstOrDefaultAsync(x => x.State_Name == "Deshabilitado");
                var id = Guid.NewGuid();

                await _context.Devices.AddAsync(new Device()
                {
                    Id = id,
                    Device_Name = sensor.Device_Name,
                    Mark = sensor.Mark,
                    SN = sensor.SN,
                    Description = sensor.Description,
                    System_TypeId = sensor.System_TypeId,
                    Device_TypeId = type.Id,
                    Device_StatusId = state.Id

                });

                await _context.ControlRules.AddAsync(new ControlRules()
                {
                    Id = Guid.NewGuid(),
                    Min = sensor.Min,
                    Max = sensor.Max,
                    DeviceId = id
                });

                await _context.Measurement_Types.AddAsync(new Measurement_Type()
                {
                    Id = Guid.NewGuid(),
                    Name = sensor.Device_Name,
                    Unit_MeasurementId = sensor.UnitId
                });

                await _context.SaveChangesAsync();

                return new ServiceResponse()
                {
                    Result = ServiceResponseType.Succeded,
                    InformationMessage = "Device add Correct"
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

        public async Task<ServiceResponse> AddActuator(ActuatorCreatePostDto actuator)
        {
            try
            {
                var type = await _context.Device_Types.FirstOrDefaultAsync(x => x.Device_Type_Name == "Actuador");
                var state = await _context.Device_States.FirstOrDefaultAsync(x => x.State_Name == "Deshabilitado");
                var id = Guid.NewGuid();

                await _context.Devices.AddAsync(new Device()
                {
                    Id = id,
                    Device_Name = actuator.Device_Name,
                    Mark = actuator.Mark,
                    SN = actuator.SN,
                    Description = actuator.Description,
                    System_TypeId = actuator.System_TypeId,
                    Device_TypeId = type.Id,
                    Device_StatusId = state.Id

                });

                await _context.ActuatorModes.AddAsync(new ActuatorMode()
                {
                    Id = Guid.NewGuid(),
                    SupportsAuto = actuator.Auto,
                    IsAutoMode = false,
                    DeviceId = id,
                });

                await _context.SaveChangesAsync();

                return new ServiceResponse()
                {
                    Result = ServiceResponseType.Succeded,
                    InformationMessage = "Device add Correct"
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

        public async Task<ServiceResponse> AddDevice(string deviceName, string mark, string sn, string description, Guid systemTypeId, Guid deviceTypeId, Guid deviceStatusId)
        {
            try
            {
                await _context.Devices.AddAsync(new Device()
                {
                    Id = Guid.NewGuid(),
                    Device_Name = deviceName,
                    Mark = mark,
                    SN = sn,
                    Description = description,
                    System_TypeId = systemTypeId,
                    Device_TypeId = deviceTypeId,
                    Device_StatusId = deviceStatusId

                });
                await _context.SaveChangesAsync();

                return new ServiceResponse()
                {
                    Result = ServiceResponseType.Succeded,
                    InformationMessage = "Device add Correct"
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

        public async Task<Device?> GetDevice(Guid DeviceId)
        {
            return await _context.Devices.FindAsync(DeviceId);
        }

        public async Task<IEnumerable<Device>> GetDevices()
        {
            return await _context.Devices.ToListAsync();
        }

        public async Task<IEnumerable<DeviceRequest>> GetActuators()
        {
            var actuator = await _context.Device_Types.FirstOrDefaultAsync(x => x.Device_Type_Name == "Actuador");

            var state = await _context.Device_States.FirstOrDefaultAsync(x => x.State_Name == "Deshabilitado");

            var devices = await _context.Devices.Include(x => x.System_Type).Include(x => x.Device_Status).Where(d => d.Device_TypeId == actuator.Id).Where(d => d.Device_StatusId != state.Id).ToListAsync();

            var devActRequest = _mapper.Map<IEnumerable<DeviceRequest>>(devices);

            return devActRequest;
        }

        public async Task<IEnumerable<DeviceDto>> GetAllActuators()
        {
            var actuator = await _context.Device_Types.FirstOrDefaultAsync(x => x.Device_Type_Name == "Actuador");

            var devices = await _context.Devices
                .Include(x => x.System_Type)
                .Include(x => x.Device_Status)
                .Where(d => d.Device_TypeId == actuator.Id)
                .Select(d => new DeviceDto
                {
                    Id = d.Id,
                    Device_Name = d.Device_Name,
                    Mark = d.Mark,
                    SN = d.SN,
                    Description = d.Description,
                    System_TypeId = d.System_Type.Id,
                    Device_StatusId = d.Device_Status.Id,
                    Device_StatusName = d.Device_Status.State_Name,
                    Auto = _context.ActuatorModes.Where(w => w.DeviceId == d.Id).FirstOrDefault().SupportsAuto
                })
                .ToListAsync();

            return devices;
        }

        public async Task<IEnumerable<SensorDto>> GetAllSensors()
        {
            var sensors = await _context.Device_Types.FirstOrDefaultAsync(x => x.Device_Type_Name == "Sensor");

            var devices = await _context.Devices
                .Include(x => x.System_Type)
                .Include(x => x.Device_Status)
                .Where(d => d.Device_TypeId == sensors.Id)
                .Select(d => new SensorDto
                {
                    Id = d.Id,
                    Device_Name = d.Device_Name,
                    Mark = d.Mark,
                    SN = d.SN,
                    Description = d.Description,
                    System_TypeId = d.System_Type.Id,
                    Device_StatusId = d.Device_Status.Id,
                    Device_StatusName = d.Device_Status.State_Name,
                    unitId = _context.Measurement_Types.Where(w => w.Name == d.Device_Name).FirstOrDefault().Unit_MeasurementId,
                    Min = _context.ControlRules.Where(w => w.DeviceId == d.Id).FirstOrDefault().Min,
                    Max = _context.ControlRules.Where(w => w.DeviceId == d.Id).FirstOrDefault().Max
                })
                .ToListAsync();

            return devices;
        }

        public async Task<List<ActuatorMode>> GetAllAsync()
        {
            return await _context.ActuatorModes.ToListAsync();
        }

        public async Task<ActuatorMode> UpdateModeAsync(Guid deviceId, bool isAutoMode)
        {
            var mode = await _context.ActuatorModes.FirstOrDefaultAsync(m => m.DeviceId == deviceId);
            if (mode == null)
            {
                throw new KeyNotFoundException("El actuador no tiene modo configurado.");
            }

            mode.IsAutoMode = isAutoMode;
            await _context.SaveChangesAsync();
            return mode;
        }

        public async Task<List<SensorMeasurementDto>> GetLastMeasurements()
        {
            var n = 20;
            var measurements = await (
                from m in _context.Measurements
                where m.Device.Device_Type.Device_Type_Name == "Sensor"
                      && m.Device.Device_Status.State_Name == "Encendido"
                orderby m.DeviceId, m.Date descending
                select new
                {
                    m.DeviceId,
                    m.Device.Device_Name,
                    m.Device.Mark,
                    m.Device.SN,
                    m.Value,
                    m.Date,
                    Unit = m.Measurement_Type.Unit_Measurement.Name
                })
                .ToListAsync();

            var grouped = measurements
                .GroupBy(x => x.DeviceId)
                .Select(g => new SensorMeasurementDto
                {
                    DeviceId = g.Key,
                    DeviceName = g.First().Device_Name,
                    Mark = g.First().Mark,//first para no recorrer todo los datos
                    SN = g.First().SN,
                    Measurements = g
                        .Take(n) // Tomar solo las últimas N
                        .Select(m => new LastMeasurementDto
                        {
                            Value = m.Value,
                            Date = m.Date.ToString("yyyy-MM-dd HH:mm:ss"),
                            Unit = m.Unit
                        })
                        .ToList()
                })
                .ToList();

            return grouped;
        }

        public async Task<List<SensorMeasurementDto>> GetAllsMeasurements()
        {
            var n = 20;
            var measurements = await (
                from m in _context.Measurements
                where m.Device.Device_Type.Device_Type_Name == "Sensor"
                      && m.Device.Device_Status.State_Name == "Encendido"
                orderby m.DeviceId, m.Date descending
                select new
                {
                    m.DeviceId,
                    m.Device.Device_Name,
                    m.Device.Mark,
                    m.Device.SN,
                    m.Value,
                    m.Date,
                    Unit = m.Measurement_Type.Unit_Measurement.Name
                })
                .ToListAsync();

            var grouped = measurements
                .GroupBy(x => x.DeviceId)
                .Select(g => new SensorMeasurementDto
                {
                    DeviceId = g.Key,
                    DeviceName = g.First().Device_Name,
                    Mark = g.First().Mark,//first para no recorrer todo los datos
                    SN = g.First().SN,
                    Measurements = g
                        .Take(n) // Tomar solo las últimas N
                        .Select(m => new LastMeasurementDto
                        {
                            Value = m.Value,
                            Date = m.Date.ToString("yyyy-MM-dd HH:mm:ss"),
                            Unit = m.Unit
                        })
                        .ToList()
                })
                .ToList();

            return grouped;
        }


        public async Task<IEnumerable<DeviceRequestSensors>> GetSensors()
        {
            var actuator = await _context.Device_Types.FirstOrDefaultAsync(x => x.Device_Type_Name == "Sensor");

            var devices = await _context.Devices
                .Include(x => x.System_Type)
                .Include(x => x.Device_Status)
                .Where(d => d.Device_TypeId == actuator.Id).Where(d => d.Device_Status.State_Name != "Deshabilitado")
                .Select(d => new DeviceRequestSensors
                {
                    Id = d.Id,
                    Device_Name = d.Device_Name,
                    SystemId = d.System_Type.Id,
                    SystemName = d.System_Type.System_Type_Name,
                    Device_StatusId = d.Device_Status.Id,
                    Device_StatusName = d.Device_Status.State_Name,
                    // Traer la unidad de la primera medición registrada
                    Unit = _context.Measurement_Types
                        .Where(m => m.Name == d.Device_Name)
                        .Select(m => m.Unit_Measurement.Name)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return devices;
        }

        public async Task<ServiceResponse> UpdateDevice(Guid DeviceId, string deviceName, string mark, string sn, string description, Guid systemTypeId, Guid deviceStatusId)
        {
            try
            {
                var device = await _context.Devices.FindAsync(DeviceId);
                if (device == null)
                {
                    return new ServiceResponse()
                    {
                        Result = ServiceResponseType.Failed,
                        ErrorMessage = "Device don't exist"
                    };
                }
                device.Device_Name = deviceName;
                device.Mark = mark;
                device.SN = sn;
                device.Description = description;
                device.System_TypeId = systemTypeId;
                device.Device_StatusId = deviceStatusId;

                _context.Devices.Update(device);

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

        public async Task<ServiceResponse> UpdateActuator(Guid DeviceId, ActuatorPostDto actuator)
        {
            try
            {
                var device = await _context.Devices.FindAsync(DeviceId);
                if (device == null)
                {
                    return new ServiceResponse()
                    {
                        Result = ServiceResponseType.Failed,
                        ErrorMessage = "Device don't exist"
                    };
                }
                device.Device_Name = actuator.Device_Name;
                device.Mark = actuator.Mark;
                device.SN = actuator.SN;
                device.Description = actuator.Description;
                device.System_TypeId = actuator.System_TypeId;

                if (actuator.Device_StatusId != null && actuator.Device_StatusId != Guid.Empty)
                {
                    device.Device_StatusId = actuator.Device_StatusId;
                }

                _context.Devices.Update(device);

                var actuatorMode = await _context.ActuatorModes.FirstOrDefaultAsync(a => a.DeviceId == DeviceId);
                if (actuatorMode == null)
                {
                    return new ServiceResponse()
                    {
                        Result = ServiceResponseType.Failed,
                        ErrorMessage = "actuator mode don't exist"
                    };
                }
                actuatorMode.SupportsAuto = actuator.Auto;

                _context.ActuatorModes.Update(actuatorMode);

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

        public async Task<ServiceResponse> UpdateSensor(Guid DeviceId, SensorPostDto sensor)
        {
            try
            {
                var device = await _context.Devices.FindAsync(DeviceId);
                if (device == null)
                {
                    return new ServiceResponse()
                    {
                        Result = ServiceResponseType.Failed,
                        ErrorMessage = "Device don't exist"
                    };
                }

                var measurement_Types = await _context.Measurement_Types.FirstOrDefaultAsync(a => a.Name == device.Device_Name);
                if (measurement_Types == null)
                {
                    return new ServiceResponse()
                    {
                        Result = ServiceResponseType.Failed,
                        ErrorMessage = "Control Rules don't exist"
                    };
                }
                measurement_Types.Name = sensor.Device_Name;
                measurement_Types.Unit_MeasurementId = sensor.UnitId;

                _context.Measurement_Types.Update(measurement_Types);
                
               
                device.Device_Name = sensor.Device_Name;
                device.Mark = sensor.Mark;
                device.SN = sensor.SN;
                device.Description = sensor.Description;
                device.System_TypeId = sensor.System_TypeId;

                if (sensor.Device_StatusId != null && sensor.Device_StatusId != Guid.Empty)
                {
                    device.Device_StatusId = sensor.Device_StatusId;
                }

                _context.Devices.Update(device);

                var controlRules = await _context.ControlRules.FirstOrDefaultAsync(a => a.DeviceId == DeviceId);
                if (controlRules == null)
                {
                    return new ServiceResponse()
                    {
                        Result = ServiceResponseType.Failed,
                        ErrorMessage = "Control Rules don't exist"
                    };
                }
                controlRules.Max = sensor.Max;
                controlRules.Min = sensor.Min;

                _context.ControlRules.Update(controlRules);

                

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

        public async Task<ServiceResponse> UpdateStateDevice(Guid DeviceId, string deviceStatus)
        {
            try
            {
                var device = await _context.Devices.FindAsync(DeviceId);
                var status = await _context.Device_States.FirstOrDefaultAsync(x => x.State_Name == deviceStatus);
                if (device == null)
                {
                    return new ServiceResponse()
                    {
                        Result = ServiceResponseType.Failed,
                        ErrorMessage = "Device don't exist"
                    };
                }
                device.Device_StatusId = status.Id;

                _context.Devices.Update(device);

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

        public async Task<ServiceResponse> DeleteDevice(Guid DeviceId)
        {
            try
            {
                var device = await _context.Devices.FindAsync(DeviceId);

                if (device == null)
                {
                    return new ServiceResponse()
                    {
                        Result = ServiceResponseType.Failed,
                        ErrorMessage = "Device don't exist"
                    };
                }
                _context.Devices.Remove(device);
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

        public async Task<ServiceResponse> turnOnOffActuator(string nameDevice, string state)
        {

            try
            {
                await _hubContext.Clients.All.SendAsync("ReceiveCommand", nameDevice, state);

                return new ServiceResponse()
                {
                    Result = ServiceResponseType.Succeded,
                    InformationMessage = "Command send Correct"
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

        public async Task<ControlRuleResponseDto?> UpdateAsync(Guid id, ControlRuleCreateDto dto)
        {
            var existing = await _context.ControlRules
                .Include(r => r.ControlRuleActuators)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (existing == null)
                return null;

            existing.Min = dto.Min;
            existing.Max = dto.Max;

            // 1. Eliminar actuadores viejos
            var oldActuators = await _context.ControlRuleActuators
                .Where(a => a.ControlRuleId == existing.Id)
                .ToListAsync();

            _context.ControlRuleActuators.RemoveRange(oldActuators);
            await _context.SaveChangesAsync(); 

            // 2. Insertar nuevos
            var newActuators = dto.Actuators.Select(aid => new ControlRuleActuator
            {
                Id = Guid.NewGuid(),
                DeviceId = aid.Id,
                ControlRuleId = existing.Id,
                TriggerType = aid.TriggerType
            }).ToList();

            await _context.ControlRuleActuators.AddRangeAsync(newActuators);
            await _context.SaveChangesAsync();

            // 3. Mapear respuesta
            return await MapToResponseDto(existing.Id);
        }



        public async Task<List<ControlRuleResponseDto>> GetByDeviceAsync(Guid deviceId)
        {
            var rules = await _context.ControlRules
                .Include(r => r.Device)
                .Include(r => r.ControlRuleActuators)
                    .ThenInclude(ca => ca.Device)
                .Where(r => r.DeviceId == deviceId)
                .ToListAsync();

            return rules.Select(r => new ControlRuleResponseDto
            {
                Id = r.Id,
                DeviceId = r.DeviceId,
                DeviceName = r.Device?.Device_Name,
                Min = r.Min,
                Max = r.Max,
                Actuators = r.ControlRuleActuators.Select(a => new ActuatorDto
                {
                    Id = a.DeviceId,
                    TriggerType = a.TriggerType ?? string.Empty
                    
                }).ToList()
            }).ToList();
        }

        private async Task<ControlRuleResponseDto> MapToResponseDto(Guid ruleId)
        {
            var rule = await _context.ControlRules
                .Include(r => r.Device)
                .Include(r => r.ControlRuleActuators)
                    .ThenInclude(ca => ca.Device)
                .FirstOrDefaultAsync(r => r.Id == ruleId);

            if (rule == null)
                throw new KeyNotFoundException("La regla no existe");

            return new ControlRuleResponseDto
            {
                Id = rule.Id,
                DeviceId = rule.DeviceId,
                DeviceName = rule.Device?.Device_Name,
                Min = rule.Min,
                Max = rule.Max,
                Actuators = rule.ControlRuleActuators
                    .Select(a => new ActuatorDto
                    {
                        Id = a.DeviceId,
                        TriggerType = a.TriggerType ?? string.Empty
                    })
                    .ToList()
            };
        }


    }
}

