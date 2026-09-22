export interface LecturaMeteo {
  id: string;
  estacionNumero: number;
  sensorId: string;
  variable: string;
  valor: number;
  valorCardinal: string | null;
  unidad: string;
  fechaHora: string;
  fechaRecepcion: string;
}
