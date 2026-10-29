// Mensajes que publica cada estación en estacionN/sd y llegan por SignalR (evento SdMeteo)

export interface DiaSd {
  d: string;   // AAAA-MM-DD
  b: number;   // tamaño del CSV en bytes
}

export interface EstadoSd {
  tipo: 'estado';
  e: number;
  sd: boolean;
  rtc: boolean;
  rtcOk: boolean;
  fuenteHora: 'NTP' | 'RTC' | 'sin_hora';
  hora: string;
  pendientes: number;
  bufferRam: number;
  enviandoDia: string;
  totalMB?: number;
  usadoMB?: number;
  totalDias?: number;
  dias?: DiaSd[];
}

export interface FinEnvioSd {
  tipo: 'fin_envio';
  e: number;
  dia: string;
  lineas: number;
  error?: string;
  nuevas?: number;
  duplicadas?: number;
  descartadas?: number;
}

export type MensajeSd = EstadoSd | FinEnvioSd;
