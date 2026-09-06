using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Models.Dto;

namespace RR_Nueva_Naturaleza.Service
{
    public interface IEventService
    {
        Task<ServiceResponse> AddEventActuator(Guid deviceId, string deviceName, string state);
        Task<Event?> GetEvent(Guid EventId);
        Task<IEnumerable<Event>> GetEvents(); // El metodo returna una coleecion de objetos (Lista)
        Task<IEnumerable<EventNotifyDto>> GetEventFalse();
        Task<ServiceResponse> UpdateEvent(Guid EventId, string notification);
        Task<ServiceResponse> DeleteEvent(Guid EventId);
        Task<ServiceResponse> NotifyAsync(Guid eventId);
        Task<ServiceResponse> MarkAsSeen(Guid eventId);
        Task<ServiceResponse> MarkAllAsSeen();
    }
}
