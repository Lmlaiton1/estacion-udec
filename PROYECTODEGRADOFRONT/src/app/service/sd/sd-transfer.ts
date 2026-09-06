import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class SdTransfer {
  private hubConnection!: signalR.HubConnection;
  private estadoSource = new BehaviorSubject<string | null>(null);
  estado$ = this.estadoSource.asObservable();

  startConnection(): void {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:5005/Hubs/SdTransfer')
      .withAutomaticReconnect()
      .build();

    this.hubConnection
      .start()
      .then(() => console.log('✅ Conectado al hub SD'))
      .catch(err => console.error('❌ Error conectando al hub SD:', err));

    this.hubConnection.on('EstadoTransferenciaSD', (estado: string) => {
      console.log(`📡 Estado transferencia SD: ${estado}`);
      this.estadoSource.next(estado);
    });
  }
}
