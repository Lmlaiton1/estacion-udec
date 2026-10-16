import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { EstadisticaPronostico, PronosticoFranjaMeteo, PronosticoMeteo } from '../../models/PronosticoMeteo';
import { Responses } from '../../models/Response';

@Injectable({
  providedIn: 'root'
})
export class PronosticoService {
  private readonly apiUrl = environment.endPoint + 'Api/ApiPronosticoMeteo';

  constructor(private _http: HttpClient) { }

  // Reentrena con las lecturas recientes y pronostica el día siguiente (tarda unos segundos)
  public generar(): Observable<Responses> {
    return this._http.post<Responses>(`${this.apiUrl}/Generar`, {});
  }

  // null cuando todavía no se ha generado ningún pronóstico (204)
  public getUltimo(): Observable<PronosticoMeteo | null> {
    return this._http.get<PronosticoMeteo | null>(`${this.apiUrl}/Ultimo`);
  }

  public getFranjas(dias = 7): Observable<PronosticoFranjaMeteo[]> {
    const params = new HttpParams().set('dias', dias);
    return this._http.get<PronosticoFranjaMeteo[]>(`${this.apiUrl}/Franjas`, { params });
  }

  public calificar(franjaId: string, condicionReal: string): Observable<Responses> {
    return this._http.post<Responses>(`${this.apiUrl}/Calificar`, { franjaId, condicionReal });
  }

  public getEstadisticas(): Observable<EstadisticaPronostico> {
    return this._http.get<EstadisticaPronostico>(`${this.apiUrl}/Estadisticas`);
  }
}
