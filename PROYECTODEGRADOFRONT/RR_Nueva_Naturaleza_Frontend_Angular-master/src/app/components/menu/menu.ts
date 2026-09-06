import { Component, EventEmitter, Output } from '@angular/core';
import { MATERIAL_MODULES } from '../../shared/material';
import { Authservice } from '../../service/auth/authservice';
import { UserPost } from '../../models/UserPost';
import { Rolservice } from '../../service/rol/rolservice';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { RouterOutlet } from '@angular/router';
import { EventService } from '../../service/event/event-service';
import { EventNotify } from '../../models/EventNotify';

@Component({
  selector: 'app-menu',
  imports: [MATERIAL_MODULES, CommonModule, RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './menu.html',
  styleUrl: './menu.scss'
})
export class Menu {

  showFiller = false;
  isDrawerOpen = false;
  user!: UserPost | null;
  rolUsuario: string | null = null;
  notify: EventNotify[] = [];
  unseenCount: number = 0;

  @Output() menuClicked = new EventEmitter<boolean>();
  constructor(public _authService: Authservice, public _RolService: Rolservice, public _eventService: EventService) {

    this.user = this._authService.getTokenUserInfo();
    if (this.user?.rolId) {
      this._RolService.getRol(this.user.rolId).subscribe(data => {
        this.rolUsuario = data.name;
      });
    }
  }

  ngOnInit(): void {
    // Iniciar conexión SignalR
    this._eventService.startConnection();

    // Suscribirse a nuevas notificaciones
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
      console.log("Notificaciones no vistas cargadas:", this.notify);
      this.updateUnseenCount(); // actualizar badge
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

    // Llamada al back para actualizar en bloque
    this._eventService.markAllAsSeen().subscribe(() => {
      this.updateUnseenCount();
      this.notify = [];
    });
  }

  esAdministrador(): boolean {
    return this.rolUsuario === 'Administrador';
  }

  logOut() {
    this._authService.logout();
  }

}
