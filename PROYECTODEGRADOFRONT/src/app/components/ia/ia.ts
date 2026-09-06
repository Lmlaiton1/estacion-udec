import { Component } from '@angular/core';
import { MATERIAL_MODULES } from '../../shared/material';

declare global {
  interface Window {
    electronAPI?: {
      runIA: () => void;
    };
  }
}

@Component({
  selector: 'app-ia',
  imports: [MATERIAL_MODULES],
  standalone: true,
  templateUrl: './ia.html',
  styleUrls: ['./ia.scss']
})
export class Ia {
  iniciarIA() {
    if (window.electronAPI) {
      window.electronAPI.runIA();
      alert('La IA se está iniciando. Puedes continuar usando el sistema.');
    } else {
      alert('No se encontró la API de Electron. Asegúrate de ejecutar la app empaquetada.');
    }
  }
}
