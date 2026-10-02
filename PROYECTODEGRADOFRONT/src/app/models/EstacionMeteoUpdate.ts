export interface EstacionMeteoUpdate {
  nombre: string;
  ubicacion: string;
  descripcion?: string | null;
  activa: boolean;
  fechaInstalacion: string;
}
