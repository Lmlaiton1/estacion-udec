// Pronóstico IA del día siguiente (ApiPronosticoMeteo)
export interface PronosticoFranjaMeteo {
  id: string;
  diaPronosticado: string;
  franja: string;
  orden: number;
  condicionPrevista: string;
  temperaturaMedia: number;
  temperaturaMin: number;
  temperaturaMax: number;
  humedadMedia: number;
  luzMediaLux: number;
  lluviaMm: number;
  condicionReal: string | null;
  acerto: boolean | null;
}

export interface PronosticoHora {
  fechaHora: string;
  temperatura: number | null;
  temperaturaInferior: number | null;
  temperaturaSuperior: number | null;
  humedad: number | null;
  humedadInferior: number | null;
  humedadSuperior: number | null;
  luz: number | null;
  luzInferior: number | null;
  luzSuperior: number | null;
  lluvia: number | null;
}

export interface PronosticoMeteo {
  id: string;
  fechaGeneracion: string;
  diaPronosticado: string;
  origenDatos: string;
  modeloVersion: string;
  umbralLluvia: number;
  factorSol: number;
  advertencia: string | null;
  ventanasEntrenamiento: number;
  segundosEjecucion: number;
  franjas: PronosticoFranjaMeteo[];
  horario: PronosticoHora[];
}

export interface EstadisticaPronostico {
  calificadas: number;
  aciertos: number;
  porcentajeAcierto: number;
  umbralLluvia: number;
  factorSol: number;
  calibrado: boolean;
  minimoParaCalibrar: number;
}
