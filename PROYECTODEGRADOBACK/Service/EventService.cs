using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RR_Nueva_Naturaleza.DAL;
using RR_Nueva_Naturaleza.Hubs;
using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Models.Dto;

namespace RR_Nueva_Naturaleza.Service
{
    public class EventService : IEventService
    {
        private readonly RR_Nueva_NaturalezaContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IMapper _mapper;

        public EventService(RR_Nueva_NaturalezaContext context, IHubContext<NotificationHub> hubContext, IMapper mapper) //Constructor con inyeccion de dependencias
        {
            _context = context;
            _hubContext = hubContext;
            _mapper = mapper;
        }

        public async Task<ServiceResponse> AddEventActuator(Guid deviceId, string deviceName, string state)
        {
            try
            {
                var device = await _context.Devices.FirstOrDefaultAsync(x => x.Id == deviceId);
                var systemType = device.System_TypeId;
                var nameImpact = await _context.Impacts.FirstOrDefaultAsync(x => x.Impact_Type == "Nulo");

                await _context.Events.AddAsync(new Event()
                {
                    Id = Guid.NewGuid(),
                    Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Notification = "El " + deviceName + " fue " + state,
                    Visto = true,
                    DeviceId = deviceId,
                    ImpactId = nameImpact.Id,
                    System_TypeId = systemType
                });
                await _context.SaveChangesAsync();

                return new ServiceResponse()
                {
                    Result = ServiceResponseType.Succeded,
                    InformationMessage = "Event add Correct"
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

        public async Task<Event?> GetEvent(Guid EventId)
        {
            return await _context.Events.FindAsync(EventId);
        }

        public async Task<IEnumerable<EventNotifyDto>> GetEventFalse()
        {
            var eventos = await _context.Events.Where(e => e.Visto == false).OrderByDescending(e => e.Date).ToListAsync();

            return _mapper.Map<IEnumerable<EventNotifyDto>>(eventos);
        }

        public async Task<IEnumerable<Event>> GetEvents()
        {
            return await _context.Events.ToListAsync();
        }

        public async Task<ServiceResponse> UpdateEvent(Guid EventId, string notification)
        {
            try
            {
                var even = await _context.Events.FindAsync(EventId);
                if (even == null)
                {
                    return new ServiceResponse()
                    {
                        Result = ServiceResponseType.Failed,
                        ErrorMessage = "Event don't exist"
                    };
                }
                even.Notification = notification;

                _context.Events.Update(even);

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

        public async Task<ServiceResponse> DeleteEvent(Guid EventId)
        {
            try
            {
                var even = await _context.Events.FindAsync(EventId);

                if (even == null)
                {
                    return new ServiceResponse()
                    {
                        Result = ServiceResponseType.Failed,
                        ErrorMessage = "event don't exist"
                    };
                }
                _context.Events.Remove(even);
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

        public async Task<ServiceResponse> NotifyAsync(Guid eventId)
        {
            try
            {
                var even = await _context.Events.FindAsync(eventId);
                if (even == null)
                {
                    return new ServiceResponse()
                    {
                        Result = ServiceResponseType.Failed,
                        ErrorMessage = "event don't exist"
                    };
                }
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
                {
                    even.Id,
                    even.Date,
                    even.Notification,
                    even.Visto
                });
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

        public async Task<ServiceResponse> MarkAsSeen(Guid id)
        {
            try
            {
                var even = await _context.Events.FindAsync(id);

                if (even == null)
                {
                    return new ServiceResponse()
                    {
                        Result = ServiceResponseType.Failed,
                        ErrorMessage = "event don't exist"
                    };
                }

                even.Visto = true;
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

        public async Task<ServiceResponse> MarkAllAsSeen()
        {
            try
            {
                var even = _context.Events.Where(e => !e.Visto).ToList();

                if (even == null)
                {
                    return new ServiceResponse()
                    {
                        Result = ServiceResponseType.Failed,
                        ErrorMessage = "event don't exist"
                    };
                }

                foreach (var ev in even)
                {
                    ev.Visto = true;
                }
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
    }
}
