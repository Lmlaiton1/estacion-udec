using System.Text.Json.Serialization;

namespace ServicioSensorica.Models
{
    // DTO del JSON compacto que publica cada estación en el topic estacionN/datos
    public class EstacionDatosDto
    {
        [JsonPropertyName("e")]
        public int Estacion { get; set; }

        [JsonPropertyName("t")]
        public string? Timestamp { get; set; }

        [JsonPropertyName("tem")]
        public decimal Temperatura { get; set; }

        [JsonPropertyName("hum")]
        public decimal Humedad { get; set; }

        [JsonPropertyName("rad")]
        public decimal Radiacion { get; set; }

        [JsonPropertyName("velV")]
        public decimal VelocidadViento { get; set; }

        [JsonPropertyName("vvp")]
        public int VelocidadVientoPulsos { get; set; }

        [JsonPropertyName("vvi")]
        public int VelocidadVientoIntervaloMs { get; set; }

        [JsonPropertyName("dirV")]
        public int DireccionGrados { get; set; }

        [JsonPropertyName("lluv")]
        public decimal Lluvia { get; set; }

        [JsonPropertyName("llup")]
        public int LluviaPulsos { get; set; }
    }
}
