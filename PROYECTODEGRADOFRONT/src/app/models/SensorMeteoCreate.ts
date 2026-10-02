export interface SensorMeteoCreate {
  id: string;
  nombre: string;
  tipo: string;
  variables: string;
  descripcion?: string | null;
}
