using Microsoft.AspNetCore.SignalR;

namespace RR_Nueva_Naturaleza.Hubs
{
    public class SdTransferHub : Hub
    {
        public async Task NotificarEstadoTransferencia(string estado)
        {
            await Clients.All.SendAsync("EstadoTransferenciaSD", estado);
        }
    }
}
