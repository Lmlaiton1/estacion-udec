using Microsoft.AspNetCore.SignalR;

namespace RR_Nueva_Naturaleza.Hubs
{
    // Canal en tiempo real de las estaciones meteorológicas (todo por la red local, sin internet).
    // - El worker invoca NotificarLecturaMeteo y el hub reenvía a los navegadores.
    // - El backend emite "ComandoReinicioEstacion" y el worker publica RESET en estacionN/comando.
    // - El backend emite "ComandoSdEstacion" (SD_ESTADO | SD_ENVIAR:AAAA-MM-DD) y el worker lo publica en estacionN/comando.
    // - El worker invoca NotificarSdMeteo con lo que responde la placa en estacionN/sd y el hub lo reenvía a la web.
    public class WeatherHub : Hub
    {
        // Eventos que escucha el frontend
        public const string EventoLectura = "LecturaMeteo";
        public const string EventoSd = "SdMeteo";

        // Eventos que escucha el worker
        public const string EventoComandoReinicio = "ComandoReinicioEstacion";
        public const string EventoComandoSd = "ComandoSdEstacion";

        public async Task NotificarLecturaMeteo(object lectura)
        {
            await Clients.Others.SendAsync(EventoLectura, lectura);
        }

        // mensajeJson: el JSON de estacionN/sd como texto (estado de la SD o fin de un envío)
        public async Task NotificarSdMeteo(string mensajeJson)
        {
            await Clients.Others.SendAsync(EventoSd, mensajeJson);
        }
    }
}
