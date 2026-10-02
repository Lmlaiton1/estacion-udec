namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class LecturaMeteoPdfRequestDto
    {
        // Null = todas las estaciones
        public int? EstacionNumero { get; set; }

        // Null o vacío = todas las variables
        public string? Variable { get; set; }

        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }

        // Rango horario aplicado dentro de cada día del rango de fechas. Null = sin filtro de hora.
        public TimeSpan? HoraInicio { get; set; }
        public TimeSpan? HoraFin { get; set; }
    }
}
