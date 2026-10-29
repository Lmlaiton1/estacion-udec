namespace RR_Nueva_Naturaleza.Models.Dto
{
    // Sensor o actuador del sistema de acuaponía que se puede vincular a un cultivo
    public class DispositivoNuevaNaturalezaDto
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty; // "Sensor" | "Actuador"
        public string Sistema { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        // Solo actuadores: si acuaponía también lo maneja con sus propias reglas automáticas
        public bool ModoAutomaticoAcuaponia { get; set; }
        // Solo sensores: última medición registrada
        public decimal? UltimoValor { get; set; }
        public DateTime? FechaUltimoValor { get; set; }
    }
}
