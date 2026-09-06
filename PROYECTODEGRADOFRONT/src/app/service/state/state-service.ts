import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class StateService {
  private hubConnection!: signalR.HubConnection;

  private estadoDosificadorSource = new BehaviorSubject<{ device: string, estado: string } | null>(null);
  estadoCambio$ = this.estadoDosificadorSource.asObservable();

  constructor() {}

  public startConnection(): void {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:5005/hubs/State')
      .withAutomaticReconnect()
      .build();

    this.hubConnection
      .start()
      .then(() => console.log('✅ Conectado al hub de dosificadores'))
      .catch(err => console.log('❌ Error conectando al hub:', err));

    this.hubConnection.on('EstadoDosificadorActualizado', (device: string, estado: string) => {
      console.log(`📡 Estado recibido: ${device} -> ${estado}`);
      this.estadoDosificadorSource.next({ device, estado });
    });
  }
}
