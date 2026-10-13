using Microsoft.AspNetCore.SignalR;

namespace RR_Nueva_Naturaleza.Hubs
{
    // Canal en tiempo real del control de cultivo (todo por la red local, sin internet).
    // - El worker invoca los métodos Notificar* y el hub reenvía a los navegadores.
    // - El backend emite "ComandoActuadorCultivo" y el worker lo publica por MQTT al ESP.
    public class CultivoHub : Hub
    {
        // Eventos que escucha el frontend
        public const string EventoAlarma = "AlarmaCultivo";
        public const string EventoActuador = "ActuadorCultivo";
        public const string EventoLectura = "LecturaCultivo";

        // Evento que escucha el worker
        public const string EventoComandoActuador = "ComandoActuadorCultivo";

        public async Task NotificarAlarmaCultivo(object alarma)
        {
            await Clients.Others.SendAsync(EventoAlarma, alarma);
        }

        public async Task NotificarActuadorCultivo(object actuador)
        {
            await Clients.Others.SendAsync(EventoActuador, actuador);
        }

        public async Task NotificarLecturaCultivo(object lectura)
        {
            await Clients.Others.SendAsync(EventoLectura, lectura);
        }
    }
}
