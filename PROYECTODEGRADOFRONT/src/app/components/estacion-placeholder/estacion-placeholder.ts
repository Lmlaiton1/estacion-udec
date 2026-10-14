import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Subscription } from 'rxjs';
import { MATERIAL_MODULES } from '../../shared/material';
import { Authservice } from '../../service/auth/authservice';
import { Rolservice } from '../../service/rol/rolservice';
import { EventService } from '../../service/event/event-service';
import { CultivoService } from '../../service/cultivo/cultivo-service';
import { AlarmaAviso } from '../../service/cultivo/alarma-aviso';
import { UserPost } from '../../models/UserPost';
import { EventNotify } from '../../models/EventNotify';
import { AlarmaCultivoEnVivo } from '../../models/Cultivo';

@Component({
  selector: 'app-estacion-placeholder',
  imports: [CommonModule, MATERIAL_MODULES, RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './estacion-placeholder.html',
  styleUrl: './estacion-placeholder.scss'
})
export class EstacionPlaceholder implements OnInit, OnDestroy {
  user: UserPost | null = null;
  rolUsuario: string | null = null;
  notify: EventNotify[] = [];
  unseenCount: number = 0;
  alarmasCultivoAbiertas = 0;
  private subAlarmas?: Subscription;

  constructor(
    private _authService: Authservice,
    private _rolService: Rolservice,
    private _eventService: EventService,
    private _cultivoService: CultivoService,
    private _alarmaAviso: AlarmaAviso,
    private _snackBar: MatSnackBar,
    private _router: Router
  ) {
    this.user = this._authService.getTokenUserInfo();
    if (this.user?.rolId) {
      this._rolService.getRol(this.user.rolId).subscribe(data => {
        this.rolUsuario = data.name;
        if (this.puedeVerCultivo()) this.escucharAlarmasCultivo();
      });
    }
  }

  ngOnDestroy(): void {
    this.subAlarmas?.unsubscribe();
  }

  // Primer nivel de aviso: en cualquier pantalla del módulo suena, aparece un mensaje
  // y, si el permiso está dado, una notificación de escritorio.
  private escucharAlarmasCultivo(): void {
    this._cultivoService.iniciarConexion();
    this.contarAlarmasCultivo();

    this.subAlarmas = this._cultivoService.alarma$.subscribe((alarma: AlarmaCultivoEnVivo) => {
      this.contarAlarmasCultivo();
      if (alarma.tipo !== 'Nueva') return;

      const irAlarmas = () => this._router.navigate(['/estacion/Cultivo']);
      this._alarmaAviso.avisar(alarma, irAlarmas);
      this._snackBar.open(`⚠ ${alarma.mensaje}`, 'Ver', { duration: 15000, panelClass: 'snack-alarma' })
        .onAction().subscribe(irAlarmas);
    });
  }

  private contarAlarmasCultivo(): void {
    this._cultivoService.getAlarmas(null, true).subscribe({
      next: data => this.alarmasCultivoAbiertas = data.length,
      error: () => this.alarmasCultivoAbiertas = 0
    });
  }

  puedeVerCultivo(): boolean {
    return this.rolUsuario === 'Administrador' || this.rolUsuario === 'Operario';
  }

  ngOnInit(): void {
    this._eventService.startConnection();

    this._eventService.onNotification((event: any) => {
      const notif: EventNotify = {
        id: event.id,
        date: event.date,
        notification: event.notification,
        visto: event.visto
      };

      this.notify.unshift(notif);
      this.updateUnseenCount();
    });

    this._eventService.GetEventsFalse().subscribe(data => {
      this.notify = data.sort(
        (a, b) => new Date(b.date).getTime() - new Date(a.date).getTime()
      );
      this.updateUnseenCount();
    });
  }

  private updateUnseenCount(): void {
    this.unseenCount = this.notify.filter(n => !n.visto).length;
  }

  markOneSeen(id: string): void {
    const notifIndex = this.notify.findIndex(n => n.id === id);
    if (notifIndex !== -1) {
      this._eventService.markAsSeen(id).subscribe(() => {
        this.notify.splice(notifIndex, 1);
        this.updateUnseenCount();
      });
    }
  }

  markAllSeen(): void {
    this.notify.forEach(n => n.visto = true);

    this._eventService.markAllAsSeen().subscribe(() => {
      this.updateUnseenCount();
      this.notify = [];
    });
  }

  esAdministrador(): boolean {
    return this.rolUsuario === 'Administrador';
  }

  puedeVerEstadistica(): boolean {
    return this.rolUsuario === 'Administrador' || this.rolUsuario === 'Operario';
  }

  logOut() {
    this._authService.logout();
  }
}
