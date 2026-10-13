import { Injectable } from '@angular/core';
import { AlarmaCultivoEnVivo } from '../../models/Cultivo';

// Primer nivel de aviso: notificación de escritorio (como la de WhatsApp) + sonido.
// Funciona sin internet, pero el navegador solo la permite en localhost o HTTPS,
// y la pestaña del sistema debe estar abierta (puede estar minimizada).
@Injectable({
  providedIn: 'root'
})
export class AlarmaAviso {
  private audio?: AudioContext;

  get soportado(): boolean {
    return typeof window !== 'undefined' && 'Notification' in window;
  }

  // false cuando se entra por http://IP: el navegador bloquea las notificaciones
  get contextoSeguro(): boolean {
    return typeof window !== 'undefined' && window.isSecureContext;
  }

  get permiso(): NotificationPermission | 'no-soportado' {
    return this.soportado ? Notification.permission : 'no-soportado';
  }

  // Debe llamarse desde un clic del usuario (Firefox y Safari lo exigen)
  async solicitarPermiso(): Promise<NotificationPermission | 'no-soportado'> {
    if (!this.soportado) return 'no-soportado';
    this.prepararAudio();
    if (Notification.permission === 'default') {
      return await Notification.requestPermission();
    }
    return Notification.permission;
  }

  avisar(alarma: AlarmaCultivoEnVivo, alHacerClic: () => void): void {
    this.sonar(alarma.nivelGravedad);

    if (!this.soportado || Notification.permission !== 'granted') return;

    const notificacion = new Notification(`⚠ Alarma ${alarma.nivelGravedad ?? ''} — ${alarma.cultivoNombre ?? 'Cultivo'}`, {
      body: alarma.mensaje ?? '',
      tag: alarma.id,
      // Las de gravedad alta se quedan en pantalla hasta que alguien las cierre
      requireInteraction: alarma.nivelGravedad === 'Alta'
    });

    notificacion.onclick = () => {
      window.focus();
      alHacerClic();
      notificacion.close();
    };
  }

  // Pitidos generados con Web Audio: no requiere archivos de sonido
  private sonar(nivel?: string): void {
    try {
      this.prepararAudio();
      if (!this.audio) return;

      const pitidos = nivel === 'Alta' ? 3 : nivel === 'Media' ? 2 : 1;
      const inicio = this.audio.currentTime;

      for (let i = 0; i < pitidos; i++) {
        const osc = this.audio.createOscillator();
        const vol = this.audio.createGain();
        osc.type = 'square';
        osc.frequency.value = 880;
        vol.gain.value = 0.15;
        osc.connect(vol).connect(this.audio.destination);
        osc.start(inicio + i * 0.35);
        osc.stop(inicio + i * 0.35 + 0.2);
      }
    } catch {
      // Sin audio disponible: se sigue mostrando la notificación visual
    }
  }

  private prepararAudio(): void {
    if (!this.audio && typeof AudioContext !== 'undefined') {
      this.audio = new AudioContext();
    }
    if (this.audio?.state === 'suspended') {
      this.audio.resume().catch(() => { });
    }
  }
}
