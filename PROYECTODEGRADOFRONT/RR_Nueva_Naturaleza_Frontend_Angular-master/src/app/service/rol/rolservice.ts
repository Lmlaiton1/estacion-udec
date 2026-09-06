import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Rol } from '../../models/Rol';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class Rolservice {
  private readonly apiUrl = environment.endPoint;
  private complement = "Api/ApiRol";

  constructor(private _http: HttpClient) { }

  public getRols(): Observable<Rol[]> {
    return this._http.get<Rol[]>(this.apiUrl + this.complement);
  }
  
  public getRol(id: string): Observable<Rol> {
    return this._http.get<Rol>(`${this.apiUrl}${this.complement}/obtener/${id}`);
  } 
}
