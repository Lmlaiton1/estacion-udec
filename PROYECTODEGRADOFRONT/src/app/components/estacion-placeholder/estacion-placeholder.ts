import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MATERIAL_MODULES } from '../../shared/material';
import { Authservice } from '../../service/auth/authservice';
import { Rolservice } from '../../service/rol/rolservice';
import { EventService } from '../../service/event/event-service';
import { UserPost } from '../../models/UserPost';
import { EventNotify } from '../../models/EventNotify';

@Component({
  selector: 'app-estacion-placeholder',
  imports: [CommonModule, MATERIAL_MODULES, RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './estacion-placeholder.html',
  styleUrl: './estacion-placeholder.scss'
})
export class EstacionPlaceholder implements OnInit {
  user: UserPost | null = null;
  rolUsuario: string | null = null;
  notify: EventNotify[] = [];
  unseenCount: number = 0;

  constructor(
    private _authService: Authservice,
    private _rolService: Rolservice,
    private _eventService: EventService
  ) {
    this.user = this._authService.getTokenUserInfo();
    if (this.user?.rolId) {
      this._rolService.getRol(this.user.rolId).subscribe(data => {
        this.rolUsuario = data.name;
      });
    }
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

  logOut() {
    this._authService.logout();
  }
}
