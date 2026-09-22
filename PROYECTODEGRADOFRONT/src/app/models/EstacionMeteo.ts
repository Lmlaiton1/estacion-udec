export interface EstacionMeteo {
  id: string;
  numero: number;
  nombre: string;
  ubicacion: string;
  descripcion: string | null;
  activa: boolean;
  fechaInstalacion: string;
}
