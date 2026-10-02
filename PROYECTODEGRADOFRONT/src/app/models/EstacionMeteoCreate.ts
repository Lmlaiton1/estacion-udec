export interface EstacionMeteoCreate {
  numero: number;
  nombre: string;
  ubicacion: string;
  descripcion?: string | null;
  activa: boolean;
  fechaInstalacion: string;
}
