export interface SensorMeteo {
  id: string;
  nombre: string;
  tipo: string;
  variables: string;
  descripcion: string | null;
  activo: boolean;
}
