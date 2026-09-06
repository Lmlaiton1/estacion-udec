export interface ChecklistDetail {
  dispositivoId: string;       // ID del dispositivo
  estado: boolean | null;             // true = Encendido, false = Apagado
  medicionSensor?: number | null;  
  medicionManual?: number | null;
}

export interface Checklist {
  usuarioId: string;           // GUID del usuario
  fecha: string;               // ISO date string
  observacion: string;         // Observación general
  detalles: ChecklistDetail[]; // Sensores + Actuadores
}
