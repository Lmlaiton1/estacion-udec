export interface UmbralMeteo {
  id: string;
  estacionNumero: number | null;
  variable: string;
  valorMin: number | null;
  valorMax: number | null;
  nivelGravedad: string;
  activo: boolean;
  descripcion: string | null;
  creadoPor: string;
  fechaCreacion: string;
}
