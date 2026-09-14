using RR_Nueva_Naturaleza.Models;

namespace RR_Nueva_Naturaleza.Service
{
    public interface ILecturaMeteoService
    {
        Task<IEnumerable<LecturaMeteo>> GetLecturas();
        Task<LecturaMeteo?> GetLectura(Guid id);
        Task<IEnumerable<LecturaMeteo>> GetLecturasByEstacion(int numero);
        Task<IEnumerable<LecturaMeteo>> GetLecturasByEstacionAndVariable(int numero, string variable);
    }
}
