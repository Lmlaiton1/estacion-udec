namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class PronosticoMeteoDto
    {
        public Guid Id { get; set; }
        public DateTime FechaGeneracion { get; set; }
        public DateTime DiaPronosticado { get; set; }
        public DateTime OrigenDatos { get; set; }
        public string ModeloVersion { get; set; } = string.Empty;
        public decimal UmbralLluvia { get; set; }
        public decimal FactorSol { get; set; }
        public string? Advertencia { get; set; }
        public int VentanasEntrenamiento { get; set; }
        public decimal SegundosEjecucion { get; set; }
        public List<PronosticoFranjaMeteoDto> Franjas { get; set; } = new();
        // 48 horas desde la hora siguiente al origen; se arma en el servicio a partir de Predicciones_Meteo
        public List<PronosticoHoraDto> Horario { get; set; } = new();
    }

    public class PronosticoFranjaMeteoDto
    {
        public Guid Id { get; set; }
        public DateTime DiaPronosticado { get; set; }
        public string Franja { get; set; } = string.Empty;
        public int Orden { get; set; }
        public string CondicionPrevista { get; set; } = string.Empty;
        public decimal TemperaturaMedia { get; set; }
        public decimal TemperaturaMin { get; set; }
        public decimal TemperaturaMax { get; set; }
        public decimal HumedadMedia { get; set; }
        public decimal LuzMediaLux { get; set; }
        public decimal LluviaMm { get; set; }
        public string? CondicionReal { get; set; }
        public bool? Acerto { get; set; }
    }

    public class PronosticoHoraDto
    {
        public DateTime FechaHora { get; set; }
        public decimal? Temperatura { get; set; }
        public decimal? TemperaturaInferior { get; set; }
        public decimal? TemperaturaSuperior { get; set; }
        public decimal? Humedad { get; set; }
        public decimal? HumedadInferior { get; set; }
        public decimal? HumedadSuperior { get; set; }
        public decimal? Luz { get; set; }
        public decimal? LuzInferior { get; set; }
        public decimal? LuzSuperior { get; set; }
        public decimal? Lluvia { get; set; }
    }

    // Lo que el usuario observó en la franja: Soleado, Nublado o Lluvioso
    public class RetroalimentacionMeteoCreateDto
    {
        public Guid FranjaId { get; set; }
        public string CondicionReal { get; set; } = string.Empty;
    }

    public class EstadisticaPronosticoDto
    {
        public int Calificadas { get; set; }
        public int Aciertos { get; set; }
        public decimal PorcentajeAcierto { get; set; }
        public decimal UmbralLluvia { get; set; }
        public decimal FactorSol { get; set; }
        // false mientras haya menos de MinimoParaCalibrar calificaciones (se usan los valores por defecto)
        public bool Calibrado { get; set; }
        public int MinimoParaCalibrar { get; set; }
    }
}
