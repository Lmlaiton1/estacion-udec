import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { EventCreate } from '../../models/EventCreate';
import * as signalR from '@microsoft/signalr';
import { EventNotify } from '../../models/EventNotify';

@Injectable({
  providedIn: 'root'
})
export class EventService {
  private readonly apiUrl = environment.endPoint;
  private complement = "Api/ApiEvent";
  private hubConnection!: signalR.HubConnection;

  constructor(private _http: HttpClient) { }

  public addEvent(event: EventCreate): Observable<EventCreate> {
    return this._http.post<EventCreate>(this.apiUrl + this.complement, event);
  }

  public GetEventsFalse(): Observable<EventNotify[]> {
    return this._http.get<EventNotify[]>(this.apiUrl + this.complement + '/GetEventsFalse');
  }

  public startConnection(): void {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${ this.apiUrl }Hubs/Notification`, {   
        withCredentials: true
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection
      .start()
      .then(() => console.log('Conexión SignalR iniciada 🚀'))
      .catch(err => console.error('Error al conectar con SignalR: ', err));
  }

  // Escuchar notificaciones
  public onNotification(callback: (notification: EventNotify) => void): void {
    this.hubConnection.on('ReceiveNotification', (notification: EventNotify) => {
      console.log("🔔 Notificación recibida:", notification);
      callback(notification);
    });
  }

  public markAsSeen(id: string): Observable<any> {
    return this._http.put<any>(`${this.apiUrl}${this.complement}/MarkAsSeen/${id}`, {});
  }
  public markAllAsSeen(): Observable<any> {
    return this._http.put<any>(`${this.apiUrl}${this.complement}/MarkAllAsSeen`, {});
  }

}
