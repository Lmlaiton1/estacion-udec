import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Rol } from '../../models/Rol';
import { environment } from '../../../environments/environment';
import { UserCreate } from '../../models/UserCreate';
import { IUser } from '../../models/IUser';

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private readonly apiUrl = environment.endPoint;
  private complement = "Api/ApiUser";

  constructor(private _http: HttpClient) { }

  public addUser(user: UserCreate): Observable<UserCreate> {
    return this._http.post<UserCreate>(this.apiUrl + this.complement, user);
  }

  public getRols(): Observable<Rol[]> {
    return this._http.get<Rol[]>(this.apiUrl + this.complement);
  }

  public getRol(id: string): Observable<Rol> {
    return this._http.get<Rol>(`${this.apiUrl}${this.complement}/obtener/${id}`);
  }

  public GetAllUsers(): Observable<IUser[]> {
    return this._http.get<IUser[]>(this.apiUrl + "Api/ApiUser");
  }
}
