import { ChecklistDetailDto } from './ChecklistDetailDto';

export interface ChecklistHeaderDto {
  usuarioId: string;      
  fecha: string;           
  observacion?: string | null;
  detalles: ChecklistDetailDto[];  
}