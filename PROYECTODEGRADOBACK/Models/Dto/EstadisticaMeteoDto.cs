namespace RR_Nueva_Naturaleza.Models.Dto
{
    // Medidas de tendencia central y dispersión sobre las últimas N lecturas de una variable
    public class EstadisticaMeteoDto
    {
        public int EstacionNumero { get; set; }
        public string Variable { get; set; } = string.Empty;
        public string Unidad { get; set; } = string.Empty;

        // Cantidad pedida y cantidad realmente encontrada (puede haber menos lecturas)
        public int CantidadSolicitada { get; set; }
        public int CantidadUsada { get; set; }

        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }

        public decimal? Media { get; set; }
        public decimal? Mediana { get; set; }

        // Puede haber varias modas (multimodal). Vacía si ningún valor se repite.
        public List<decimal> Modas { get; set; } = new();
        public int FrecuenciaModa { get; set; }

        public decimal? Minimo { get; set; }
        public decimal? Maximo { get; set; }
        public decimal? Rango { get; set; }
        public decimal? DesviacionEstandar { get; set; }

        // Valores en orden cronológico para graficar el histograma en el cliente
        public List<decimal> Valores { get; set; } = new();
    }
}
