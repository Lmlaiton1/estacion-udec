import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { LecturaMeteo } from '../../models/LecturaMeteo';
import { EstacionMeteo } from '../../models/EstacionMeteo';
import { SensorMeteo } from '../../models/SensorMeteo';
import { EstacionMeteoCreate } from '../../models/EstacionMeteoCreate';
import { EstacionMeteoUpdate } from '../../models/EstacionMeteoUpdate';
import { SensorMeteoCreate } from '../../models/SensorMeteoCreate';
import { SensorMeteoUpdate } from '../../models/SensorMeteoUpdate';
import { UmbralMeteo } from '../../models/UmbralMeteo';
import { UmbralMeteoCreate } from '../../models/UmbralMeteoCreate';
import { UmbralMeteoUpdate } from '../../models/UmbralMeteoUpdate';
import { AlertaMeteo } from '../../models/AlertaMeteo';
import { LecturaMeteoPdfRequest } from '../../models/LecturaMeteoPdfRequest';
import { CambiarEstado } from '../../models/CambiarEstado';
import { AuditoriaMeteo } from '../../models/AuditoriaMeteo';
import { EstadisticaMeteo } from '../../models/EstadisticaMeteo';
import { Responses } from '../../models/Response';

@Injectable({
  providedIn: 'root'
})
export class WeatherService {
  private readonly apiUrl = environment.endPoint;
  private complement1 = 'Api/ApiLecturaMeteo/GetByEstacion';
  private complement2 = 'Api/ApiLecturaMeteo/GetByEstacionAndVariable';
  private complement3 = 'Api/ApiEstacionMeteo';
  private complement4 = 'Api/ApiEstacionMeteo/CreateEstacion';
  private complement5 = 'Api/ApiEstacionMeteo/UpdateEstacion';
  private complement6 = 'Api/ApiSensorMeteo';
  private complement7 = 'Api/ApiSensorMeteo/CreateSensor';
  private complement8 = 'Api/ApiSensorMeteo/UpdateSensor';
  private complement9 = 'Api/ApiUmbralMeteo';
  private complement10 = 'Api/ApiUmbralMeteo/CreateUmbral';
  private complement11 = 'Api/ApiUmbralMeteo/UpdateUmbral';
  private complement12 = 'Api/ApiAlertaMeteo';
  private complement13 = 'Api/ApiAlertaMeteo/MarcarVista';
  private complement14 = 'Api/ApiLecturaMeteo/GetLecturasForPdf';
  private complement15 = 'Api/ApiEstacionMeteo/CambiarEstado';
  private complement16 = 'Api/ApiSensorMeteo/CambiarEstado';
  private complement17 = 'Api/ApiAuditoriaMeteo';
  private complement18 = 'Api/ApiLecturaMeteo/Estadisticas';

  constructor(private _http: HttpClient) { }

  // fechaInicio/fechaFin quedan listos para cuando el backend soporte filtrado por rango;
  // por ahora el backend los ignora y el filtro se aplica en el cliente.
  public getByEstacion(numero: number, fechaInicio?: Date | null, fechaFin?: Date | null): Observable<LecturaMeteo[]> {
    const params = this.buildRangoParams(fechaInicio, fechaFin);
    return this._http.get<LecturaMeteo[]>(`${this.apiUrl + this.complement1}/${numero}`, { params });
  }

  public getByEstacionAndVariable(numero: number, variable: string, fechaInicio?: Date | null, fechaFin?: Date | null): Observable<LecturaMeteo[]> {
    const params = this.buildRangoParams(fechaInicio, fechaFin);
    return this._http.get<LecturaMeteo[]>(`${this.apiUrl + this.complement2}/${numero}/${variable}`, { params });
  }

  public getEstaciones(): Observable<EstacionMeteo[]> {
    return this._http.get<EstacionMeteo[]>(this.apiUrl + this.complement3);
  }

  public crearEstacion(estacion: EstacionMeteoCreate): Observable<Responses> {
    return this._http.put<Responses>(this.apiUrl + this.complement4, estacion);
  }

  public actualizarEstacion(id: string, estacion: EstacionMeteoUpdate): Observable<Responses> {
    return this._http.put<Responses>(`${this.apiUrl + this.complement5}/${id}`, estacion);
  }

  public eliminarEstacion(id: string): Observable<Responses> {
    return this._http.delete<Responses>(`${this.apiUrl + this.complement3}/${id}`);
  }

  public getSensores(): Observable<SensorMeteo[]> {
    return this._http.get<SensorMeteo[]>(this.apiUrl + this.complement6);
  }

  public crearSensor(sensor: SensorMeteoCreate): Observable<Responses> {
    return this._http.put<Responses>(this.apiUrl + this.complement7, sensor);
  }

  public actualizarSensor(id: string, sensor: SensorMeteoUpdate): Observable<Responses> {
    return this._http.put<Responses>(`${this.apiUrl + this.complement8}/${id}`, sensor);
  }

  public eliminarSensor(id: string): Observable<Responses> {
    return this._http.delete<Responses>(`${this.apiUrl + this.complement6}/${id}`);
  }

  public getUmbrales(): Observable<UmbralMeteo[]> {
    return this._http.get<UmbralMeteo[]>(this.apiUrl + this.complement9);
  }

  public crearUmbral(umbral: UmbralMeteoCreate): Observable<Responses> {
    return this._http.put<Responses>(this.apiUrl + this.complement10, umbral);
  }

  public actualizarUmbral(id: string, umbral: UmbralMeteoUpdate): Observable<Responses> {
    return this._http.put<Responses>(`${this.apiUrl + this.complement11}/${id}`, umbral);
  }

  public eliminarUmbral(id: string): Observable<Responses> {
    return this._http.delete<Responses>(`${this.apiUrl + this.complement9}/${id}`);
  }

  public getAlertas(): Observable<AlertaMeteo[]> {
    return this._http.get<AlertaMeteo[]>(this.apiUrl + this.complement12);
  }

  public marcarVistaAlerta(id: string): Observable<Responses> {
    return this._http.put<Responses>(`${this.apiUrl + this.complement13}/${id}`, {});
  }

  public getLecturasParaReportePdf(filtro: LecturaMeteoPdfRequest) {
    return this._http.post(this.apiUrl + this.complement14, filtro, {
      responseType: 'blob',
      observe: 'response' as const
    });
  }

  public cambiarEstadoEstacion(id: string, cambio: CambiarEstado): Observable<Responses> {
    return this._http.put<Responses>(`${this.apiUrl + this.complement15}/${id}`, cambio);
  }

  public cambiarEstadoSensor(id: string, cambio: CambiarEstado): Observable<Responses> {
    return this._http.put<Responses>(`${this.apiUrl + this.complement16}/${id}`, cambio);
  }

  public getAuditoriaMeteo(): Observable<AuditoriaMeteo[]> {
    return this._http.get<AuditoriaMeteo[]>(this.apiUrl + this.complement17);
  }

  public getEstadisticas(numero: number, variable: string, cantidad: number): Observable<EstadisticaMeteo> {
    const params = new HttpParams().set('cantidad', cantidad);
    return this._http.get<EstadisticaMeteo>(`${this.apiUrl + this.complement18}/${numero}/${variable}`, { params });
  }

  private buildRangoParams(fechaInicio?: Date | null, fechaFin?: Date | null): HttpParams {
    let params = new HttpParams();
    if (fechaInicio) params = params.set('fechaInicio', fechaInicio.toISOString());
    if (fechaFin) params = params.set('fechaFin', fechaFin.toISOString());
    return params;
  }
}
