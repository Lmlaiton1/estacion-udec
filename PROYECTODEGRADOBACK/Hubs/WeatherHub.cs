using Microsoft.AspNetCore.SignalR;

namespace RR_Nueva_Naturaleza.Hubs
{
    // Canal en tiempo real de las estaciones meteorológicas (todo por la red local, sin internet).
    // - El worker invoca NotificarLecturaMeteo y el hub reenvía a los navegadores.
    // - El backend emite "ComandoReinicioEstacion" y el worker publica RESET en estacionN/comando.
    public class WeatherHub : Hub
    {
        // Evento que escucha el frontend
        public const string EventoLectura = "LecturaMeteo";

        // Evento que escucha el worker
        public const string EventoComandoReinicio = "ComandoReinicioEstacion";

        public async Task NotificarLecturaMeteo(object lectura)
        {
            await Clients.Others.SendAsync(EventoLectura, lectura);
        }
    }
}
