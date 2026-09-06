import { Component, Inject } from '@angular/core';
import { MATERIAL_MODULES } from '../../shared/material';
import { MAT_DIALOG_DATA, MatDialogRef} from '@angular/material/dialog';
import { CommonModule } from '@angular/common';

type ReasonType = 'Mantenimiento' | 'Pruebas' | 'Rutina' | 'Otra';

@Component({
  selector: 'app-dialog',
  imports: [MATERIAL_MODULES, CommonModule],
  templateUrl: './dialog.html',
  styleUrl: './dialog.scss'
})
export class Dialog {
  reason: ReasonType | '' = '';
  customReason = '';

  constructor(
    @Inject(MAT_DIALOG_DATA) public data: { device_Name: string; currentState: 'Activo' | 'Inactivo'; targetState: 'Activo' | 'Inactivo' },
    private dialogRef: MatDialogRef<Dialog>
  ) {}

  isValid(): boolean {
    if (!this.reason) return false;
    if (this.reason === 'Otra') return this.customReason.trim().length >= 3;
    return true;
  }

  confirm(): void {
    const payload = {
      reasonType: this.reason as ReasonType,
      reasonText: this.reason === 'Otra' ? this.customReason.trim() : (this.reason as string),
      targetState: this.data.targetState
    };
    this.dialogRef.close(payload);
  }

  close(): void {
    this.dialogRef.close(null);
  }
}
