export interface ChecklistDetailDto {
  dispositivoId: string; 
  estado: boolean;
  medicionSensor?: number | null;
  medicionManual?: number | null;
}