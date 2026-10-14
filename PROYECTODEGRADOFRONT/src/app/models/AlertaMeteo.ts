export interface AlertaMeteo {
  id: string;
  lecturaId: string;
  umbralId: string;
  estacionNumero: number;
  variable: string;
  valorRegistrado: number;
  mensaje: string;
  nivelGravedad: string;
  fechaHora: string;
  visto: boolean;
  vistoPorId: string | null;
  vistoPorNombre: string | null;
  fechaVisto: string | null;
  accionTomada: string | null;
}
