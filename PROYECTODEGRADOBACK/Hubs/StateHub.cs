using Microsoft.AspNetCore.SignalR;

namespace RR_Nueva_Naturaleza.Hubs
{
    public class StateHub : Hub
    {
        public async Task NotificarEstadoDosificador(string device, string estado)
        {
            await Clients.All.SendAsync("EstadoDosificadorActualizado", device, estado);
        }
    }
}
