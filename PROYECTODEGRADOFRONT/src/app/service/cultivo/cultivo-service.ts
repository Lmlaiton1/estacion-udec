import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, Subject } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../../environments/environment';
import { Responses } from '../../models/Response';
import { CambiarEstado } from '../../models/CambiarEstado';
import {
  AccionarActuador, ActuadorCultivo, ActuadorCultivoCreate, ActuadorCultivoEnVivo, AlarmaCultivo, AlarmaCultivoEnVivo,
  BitacoraAlarmaCultivo, Cultivo, CultivoCreate, EtapaCultivo, EtapaCultivoCreate, EventoDispositivoCultivo,
  SensorCultivo, SensorCultivoCreate, UmbralCultivo, UmbralCultivoCreate, AuditoriaCultivo, DispositivoNuevaNaturaleza
} from '../../models/Cultivo';

@Injectable({
  providedIn: 'root'
})
export class CultivoService {
  private readonly apiUrl = environment.endPoint;
  private readonly cultivo = 'Api/ApiCultivo';
  private readonly sensor = 'Api/ApiSensorCultivo';
  private readonly actuador = 'Api/ApiActuadorCultivo';
  private readonly umbral = 'Api/ApiUmbralCultivo';
  private readonly alarma = 'Api/ApiAlarmaCultivo';
  private readonly bitacora = 'Api/ApiBitacoraCultivo';

  // Tiempo real (red local): alarmas nuevas/actualizadas y cambios de actuadores
  private hubConnection?: signalR.HubConnection;
  readonly alarma$ = new Subject<AlarmaCultivoEnVivo>();
  readonly actuador$ = new Subject<ActuadorCultivoEnVivo>();
  readonly lectura$ = new Subject<any>();

  constructor(private _http: HttpClient) { }

  // Una sola conexión para toda la app; se puede llamar varias veces sin abrir otra
  public iniciarConexion(): void {
    if (this.hubConnection) return;

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${this.apiUrl}Hubs/Cultivo`, { withCredentials: true })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('AlarmaCultivo', (a: AlarmaCultivoEnVivo) => this.alarma$.next(a));
    this.hubConnection.on('ActuadorCultivo', (a: ActuadorCultivoEnVivo) => this.actuador$.next(a));
    this.hubConnection.on('LecturaCultivo', (l: any) => this.lectura$.next(l));

    this.hubConnection.start().catch(err => console.error('Error al conectar con CultivoHub: ', err));
  }

  // Cultivos y etapas
  public getCultivos(): Observable<Cultivo[]> {
    return this._http.get<Cultivo[]>(this.apiUrl + this.cultivo);
  }

  // Sensores y actuadores de acuaponía que se pueden vincular a un cultivo
  public getDispositivosNuevaNaturaleza(): Observable<DispositivoNuevaNaturaleza[]> {
    return this._http.get<DispositivoNuevaNaturaleza[]>(`${this.apiUrl + this.cultivo}/DispositivosNuevaNaturaleza`);
  }

  public crearCultivo(cultivo: CultivoCreate): Observable<Responses> {
    return this._http.put<Responses>(`${this.apiUrl + this.cultivo}/CreateCultivo`, cultivo);
  }

  public actualizarCultivo(id: string, cultivo: CultivoCreate): Observable<Responses> {
    return this._http.put<Responses>(`${this.apiUrl + this.cultivo}/UpdateCultivo/${id}`, cultivo);
  }

  public cambiarEstadoCultivo(id: string, cambio: CambiarEstado): Observable<Responses> {
    return this._http.put<Responses>(`${this.apiUrl + this.cultivo}/CambiarEstado/${id}`, cambio);
  }

  public eliminarCultivo(id: string, motivo: string): Observable<Responses> {
    return this._http.put<Responses>(`${this.apiUrl + this.cultivo}/Eliminar/${id}`, { motivo });
  }

  public crearEtapa(etapa: EtapaCultivoCreate): Observable<Responses> {
    return this._http.put<Responses>(`${this.apiUrl + this.cultivo}/CreateEtapa`, etapa);
  }

  public actualizarEtapa(id: string, etapa: EtapaCultivoCreate): Observable<Responses> {
    return this._http.put<Responses>(`${this.apiUrl + this.cultivo}/UpdateEtapa/${id}`, etapa);
  }

  public eliminarEtapa(id: string, motivo: string): Observable<Responses> {
    return this._http.put<Responses>(`${this.apiUrl + this.cultivo}/EliminarEtapa/${id}`, { motivo });
  }

  public activarEtapa(id: string, motivo: string): Observable<Responses> {
    return this._http.put<Responses>(`${this.apiUrl + this.cultivo}/ActivarEtapa/${id}`, { motivo });
  }

  public quitarEtapaActual(id: string, motivo: string): Observable<Responses> {
    return this._http.put<Responses>(`${this.apiUrl + this.cultivo}/QuitarEtapaActual/${id}`, { motivo });
  }

  // Sensores
  public getSensores(cultivoId?: string | null): Observable<SensorCultivo[]> {
    return this._http.get<SensorCultivo[]>(this.apiUrl + this.sensor, { params: this.params({ cultivoId }) });
  }

  public crearSensor(sensor: SensorCultivoCreate): Observable<Responses> {
    return this._http.put<Responses>(`${this.apiUrl + this.sensor}/CreateSensor`, sensor);
  }

  public actualizarSensor(id: string, sensor: SensorCultivoCreate): Observable<Responses> {
    return this._http.put<Responses>(`${this.apiUrl + this.sensor}/UpdateSensor/${id}`, sensor);
  }

  public eliminarSensor(id: string, motivo: string): Observable<Responses> {
    return this._http.put<Responses>(`${this.apiUrl + this.sensor}/Eliminar/${id}`, { motivo });
  }

  public cambiarEstadoSensor(id: string, cambio: CambiarEstado): Observable<Responses> {
    return this._http.put<Responses>(`${this.apiUrl + this.sensor}/CambiarEstado/${id}`, cambio);
  }

  // Actuadores
  public getActuadores(cultivoId?: string | null): Observable<ActuadorCultivo[]> {
    return this._http.get<ActuadorCultivo[]>(this.apiUrl + this.actuador, { params: this.params({ cultivoId }) });
  }

  public crearActuador(actuador: ActuadorCultivoCreate): Observable<Responses> {
    return this._http.put<Responses>(`${this.apiUrl + this.actuador}/CreateActuador`, actuador);
  }

  public actualizarActuador(id: string, actuador: ActuadorCultivoCreate): Observable<Responses> {
    return this._http.put<Responses>(`${this.apiUrl + this.actuador}/UpdateActuador/${id}`, actuador);
  }

  public eliminarActuador(id: string, motivo: string): Observable<Responses> {
    return this._http.put<Responses>(`${this.apiUrl + this.actuador}/Eliminar/${id}`, { motivo });
  }

  public accionarActuador(id: string, accion: AccionarActuador): Observable<Responses> {
    return this._http.put<Responses>(`${this.apiUrl + this.actuador}/Accionar/${id}`, accion);
  }

  public cambiarModoActuador(id: string, modo: string, motivo: string): Observable<Responses> {
    return this._http.put<Responses>(`${this.apiUrl + this.actuador}/CambiarModo/${id}`, { modo, motivo });
  }

  public cambiarEstadoActuador(id: string, cambio: CambiarEstado): Observable<Responses> {
    return this._http.put<Responses>(`${this.apiUrl + this.actuador}/CambiarEstado/${id}`, cambio);
  }

  // Umbrales
  public getUmbrales(cultivoId?: string | null): Observable<UmbralCultivo[]> {
    return this._http.get<UmbralCultivo[]>(this.apiUrl + this.umbral, { params: this.params({ cultivoId }) });
  }

  public crearUmbral(umbral: UmbralCultivoCreate): Observable<Responses> {
    return this._http.put<Responses>(`${this.apiUrl + this.umbral}/CreateUmbral`, umbral);
  }

  public actualizarUmbral(id: string, umbral: UmbralCultivoCreate): Observable<Responses> {
    return this._http.put<Responses>(`${this.apiUrl + this.umbral}/UpdateUmbral/${id}`, umbral);
  }

  public cambiarEstadoUmbral(id: string, cambio: CambiarEstado): Observable<Responses> {
    return this._http.put<Responses>(`${this.apiUrl + this.umbral}/CambiarEstado/${id}`, cambio);
  }

  public eliminarUmbral(id: string, motivo: string): Observable<Responses> {
    return this._http.put<Responses>(`${this.apiUrl + this.umbral}/Eliminar/${id}`, { motivo });
  }

  // Alarmas y su auditoría
  public getAlarmas(cultivoId?: string | null, soloAbiertas = false): Observable<AlarmaCultivo[]> {
    return this._http.get<AlarmaCultivo[]>(this.apiUrl + this.alarma, { params: this.params({ cultivoId, soloAbiertas }) });
  }

  public marcarVista(id: string): Observable<Responses> {
    return this._http.put<Responses>(`${this.apiUrl + this.alarma}/MarcarVista/${id}`, {});
  }

  public tomarControl(id: string, comentario: string): Observable<Responses> {
    return this._http.put<Responses>(`${this.apiUrl + this.alarma}/TomarControl/${id}`, { comentario });
  }

  public comentar(id: string, comentario: string): Observable<Responses> {
    return this._http.put<Responses>(`${this.apiUrl + this.alarma}/Comentar/${id}`, { comentario });
  }

  public resolver(id: string, comentario: string): Observable<Responses> {
    return this._http.put<Responses>(`${this.apiUrl + this.alarma}/Resolver/${id}`, { comentario });
  }

  // Administración de alarmas (solo Administrador)
  public modificarAlarma(id: string, nivelGravedad: string, solucion: string | null, motivo: string): Observable<Responses> {
    return this._http.put<Responses>(`${this.apiUrl + this.alarma}/UpdateAlarma/${id}`, { nivelGravedad, solucion, motivo });
  }

  public anularAlarma(id: string, motivo: string): Observable<Responses> {
    return this._http.put<Responses>(`${this.apiUrl + this.alarma}/Anular/${id}`, { motivo });
  }

  public reactivarAlarma(id: string, motivo: string): Observable<Responses> {
    return this._http.put<Responses>(`${this.apiUrl + this.alarma}/Reactivar/${id}`, { motivo });
  }

  public eliminarAlarma(id: string, motivo: string): Observable<Responses> {
    return this._http.put<Responses>(`${this.apiUrl + this.alarma}/Eliminar/${id}`, { motivo });
  }

  public getAuditoriaConfiguracion(cultivoId?: string | null, tipoEntidad?: string | null, desde?: string | null): Observable<AuditoriaCultivo[]> {
    return this._http.get<AuditoriaCultivo[]>(`${this.apiUrl + this.bitacora}/Configuracion`,
      { params: this.params({ cultivoId, tipoEntidad, desde }) });
  }

  public getEventos(cultivoId?: string | null, tipoDispositivo?: string | null, desde?: string | null): Observable<EventoDispositivoCultivo[]> {
    return this._http.get<EventoDispositivoCultivo[]>(`${this.apiUrl + this.bitacora}/Eventos`,
      { params: this.params({ cultivoId, tipoDispositivo, desde }) });
  }

  public getBitacoraAlarmas(cultivoId?: string | null, desde?: string | null): Observable<BitacoraAlarmaCultivo[]> {
    return this._http.get<BitacoraAlarmaCultivo[]>(`${this.apiUrl + this.bitacora}/Alarmas`,
      { params: this.params({ cultivoId, desde }) });
  }

  // Omite los filtros vacíos para no mandar "null" como texto
  private params(valores: Record<string, string | boolean | null | undefined>): HttpParams {
    let params = new HttpParams();
    Object.entries(valores).forEach(([clave, valor]) => {
      if (valor !== null && valor !== undefined && valor !== '') params = params.set(clave, String(valor));
    });
    return params;
  }
}
