using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Models.Dto;

namespace RR_Nueva_Naturaleza.Service
{
    public interface IPronosticoMeteoService
    {
        // Exporta lecturas, reentrena con Python y guarda el pronóstico del día siguiente
        Task<ServiceResponse> GenerarPronostico(Guid usuarioId);
        Task<PronosticoMeteoDto?> GetUltimo();
        // Franjas ya transcurridas de los últimos días (calificadas o pendientes de calificar)
        Task<List<PronosticoFranjaMeteoDto>> GetFranjasRecientes(int dias);
        Task<ServiceResponse> Calificar(RetroalimentacionMeteoCreateDto retro, Guid usuarioId);
        Task<EstadisticaPronosticoDto> GetEstadisticas();
    }
}
