import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { LecturaMeteo } from '../../models/LecturaMeteo';
import { EstacionMeteo } from '../../models/EstacionMeteo';

@Injectable({
  providedIn: 'root'
})
export class WeatherService {
  private readonly apiUrl = environment.endPoint;
  private complement1 = 'Api/ApiLecturaMeteo/GetByEstacion';
  private complement2 = 'Api/ApiLecturaMeteo/GetByEstacionAndVariable';
  private complement3 = 'Api/ApiEstacionMeteo';

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

  private buildRangoParams(fechaInicio?: Date | null, fechaFin?: Date | null): HttpParams {
    let params = new HttpParams();
    if (fechaInicio) params = params.set('fechaInicio', fechaInicio.toISOString());
    if (fechaFin) params = params.set('fechaFin', fechaFin.toISOString());
    return params;
  }
}
