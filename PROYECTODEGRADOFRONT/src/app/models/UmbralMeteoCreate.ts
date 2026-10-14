export interface UmbralMeteoCreate {
  estacionNumero: number | null;
  variable: string;
  valorMin: number | null;
  valorMax: number | null;
  nivelGravedad: string;
  activo: boolean;
  descripcion?: string | null;
  motivo: string; // obligatorio, queda en la auditoría
}
