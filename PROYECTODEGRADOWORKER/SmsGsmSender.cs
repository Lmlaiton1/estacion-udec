using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServicioSensorica.Worker
{
    // Envía SMS por un módem GSM (SIM7600, A7670, etc.) conectado por USB/serial al PC, con comandos AT.
    // Usa la red celular, NO internet. Queda apagado hasta poner Sms:Habilitado = true en appsettings.
    public class SmsGsmSender
    {
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _candado = new(1, 1);

        public bool Habilitado { get; }
        public IReadOnlyList<string> Destinatarios { get; }

        private readonly string _puerto;
        private readonly int _baudios;

        public SmsGsmSender(IConfiguration configuration, ILogger logger)
        {
            _logger = logger;
            Habilitado = bool.TryParse(configuration["Sms:Habilitado"], out var h) && h;
            _puerto = configuration["Sms:Puerto"] ?? "COM5";
            _baudios = int.TryParse(configuration["Sms:BaudRate"], out var b) ? b : 115200;
            Destinatarios = configuration.GetSection("Sms:Destinatarios").GetChildren()
                .Select(c => c.Value ?? string.Empty)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToList();
        }

        // Devuelve cuántos destinatarios recibieron el SMS
        public async Task<int> EnviarATodosAsync(string mensaje)
        {
            if (!Habilitado || Destinatarios.Count == 0) return 0;

            await _candado.WaitAsync();
            try
            {
                return await Task.Run(() => EnviarSincrono(mensaje));
            }
            finally
            {
                _candado.Release();
            }
        }

        private int EnviarSincrono(string mensaje)
        {
            var texto = PrepararTexto(mensaje);
            var enviados = 0;

            using var puerto = new SerialPort(_puerto, _baudios)
            {
                ReadTimeout = 2000,
                WriteTimeout = 2000,
                NewLine = "\r",
                Encoding = Encoding.ASCII
            };

            try
            {
                puerto.Open();
                Comando(puerto, "AT", "OK", 2000);
                Comando(puerto, "AT+CMGF=1", "OK", 2000); // modo texto

                foreach (var numero in Destinatarios)
                {
                    try
                    {
                        Comando(puerto, $"AT+CMGS=\"{numero}\"", ">", 5000);
                        puerto.Write(texto + (char)26); // Ctrl+Z cierra el mensaje
                        Esperar(puerto, "OK", 30000);
                        enviados++;
                        _logger.LogWarning("SMS de alarma enviado a {numero}", numero);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "No se pudo enviar el SMS a {numero}", numero);
                        puerto.Write(((char)27).ToString()); // ESC cancela un envío a medias
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error con el módem GSM en {puerto}", _puerto);
            }

            return enviados;
        }

        private static void Comando(SerialPort puerto, string comando, string esperado, int timeoutMs)
        {
            puerto.DiscardInBuffer();
            puerto.Write(comando + "\r");
            Esperar(puerto, esperado, timeoutMs);
        }

        private static void Esperar(SerialPort puerto, string esperado, int timeoutMs)
        {
            var respuesta = new StringBuilder();
            var limite = DateTime.Now.AddMilliseconds(timeoutMs);

            while (DateTime.Now < limite)
            {
                if (puerto.BytesToRead > 0)
                {
                    respuesta.Append(puerto.ReadExisting());
                    var texto = respuesta.ToString();
                    if (texto.Contains(esperado)) return;
                    if (texto.Contains("ERROR")) throw new InvalidOperationException($"El módem respondió: {texto.Trim()}");
                }
                Thread.Sleep(50);
            }

            throw new TimeoutException($"El módem no respondió '{esperado}'. Recibido: {respuesta.ToString().Trim()}");
        }

        // GSM en modo texto: sin tildes y máximo 160 caracteres
        private static string PrepararTexto(string mensaje)
        {
            var normalizado = mensaje.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in normalizado)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
                sb.Append(c == 'ñ' ? 'n' : c == 'Ñ' ? 'N' : c < 128 ? c : '?');
            }
            var texto = sb.ToString();
            return texto.Length > 160 ? texto[..160] : texto;
        }
    }
}
