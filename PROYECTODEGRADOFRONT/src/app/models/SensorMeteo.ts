export interface SensorMeteo {
  id: string;
  nombre: string;
  tipo: string;
  variables: string;
  descripcion: string | null;
  // Activo global: false = apagado en todas las estaciones
  activo: boolean;
  // Estaciones donde está desactivado puntualmente
  estacionesInactivas: number[];
}
