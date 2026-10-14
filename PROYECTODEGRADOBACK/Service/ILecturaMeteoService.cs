using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Models.Dto;

namespace RR_Nueva_Naturaleza.Service
{
    public interface ILecturaMeteoService
    {
        Task<IEnumerable<LecturaMeteo>> GetLecturas();
        Task<LecturaMeteo?> GetLectura(Guid id);
        Task<IEnumerable<LecturaMeteo>> GetLecturasByEstacion(int numero, DateTime? fechaInicio, DateTime? fechaFin);
        Task<IEnumerable<LecturaMeteo>> GetLecturasByEstacionAndVariable(int numero, string variable, DateTime? fechaInicio, DateTime? fechaFin);
        Task<IEnumerable<LecturaMeteo>> GetUltimasLecturas();
        Task<IEnumerable<LecturaMeteo>> GetLecturasParaReporte(LecturaMeteoPdfRequestDto filtro);
        Task GenerarPdf(Stream output, LecturaMeteoPdfRequestDto filtro);
        Task<EstadisticaMeteoDto> GetEstadisticas(int estacionNumero, string variable, int cantidad);
    }
}
