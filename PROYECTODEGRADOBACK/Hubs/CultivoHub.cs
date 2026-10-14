using Microsoft.AspNetCore.SignalR;

namespace RR_Nueva_Naturaleza.Hubs
{
    // Canal en tiempo real del control de cultivo (todo por la red local, sin internet).
    // - El worker invoca los métodos Notificar* y el hub reenvía a los navegadores.
    // - El backend emite "ComandoActuadorCultivo" y el worker lo publica por MQTT al ESP.
    public class CultivoHub : Hub
    {
        private readonly IHubContext<ActuatorHub> _actuatorHub;

        public CultivoHub(IHubContext<ActuatorHub> actuatorHub)
        {
            _actuatorHub = actuatorHub;
        }

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

        // Lo invoca el worker cuando un umbral acciona un actuador de Nueva Naturaleza: se reenvía al
        // hub de acuaponía y Worker.cs lo escribe al puerto serial. estado = "Activo" | "Inactivo"
        public async Task EnviarComandoNuevaNaturaleza(string dispositivo, string estado)
        {
            await _actuatorHub.Clients.All.SendAsync("ReceiveCommand", dispositivo, estado);
        }

        public async Task NotificarLecturaCultivo(object lectura)
        {
            await Clients.Others.SendAsync(EventoLectura, lectura);
        }
    }
}
