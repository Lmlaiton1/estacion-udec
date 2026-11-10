// estacionNumero null = cambiar el estado en todas las estaciones
export interface CambiarEstadoSensorMeteo {
  activo: boolean;
  motivo: string;
  estacionNumero: number | null;
}
