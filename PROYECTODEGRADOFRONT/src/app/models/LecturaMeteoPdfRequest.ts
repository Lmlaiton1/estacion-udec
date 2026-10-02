export interface LecturaMeteoPdfRequest {
  estacionNumero?: number | null;
  variable?: string | null;
  fechaInicio: string;
  fechaFin: string;
  // Formato "HH:mm:ss"
  horaInicio?: string | null;
  horaFin?: string | null;
}
