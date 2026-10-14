export interface AuditoriaMeteo {
  id: string;
  tipoEntidad: string;
  entidadId: string;
  nombreEntidad: string;
  accion: string;
  detalle: string | null;
  motivo: string;
  fecha: string;
  usuarioId: string;
  usuarioNombre: string;
}
