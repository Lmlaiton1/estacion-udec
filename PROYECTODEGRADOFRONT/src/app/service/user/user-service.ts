import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Rol } from '../../models/Rol';
import { environment } from '../../../environments/environment';
import { UserCreate } from '../../models/UserCreate';
import { IUser } from '../../models/IUser';
import { UserUpdate } from '../../models/UserUpdate';
import { Responses } from '../../models/Response';

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

  public updateUser(user: UserUpdate): Observable<Responses> {
    return this._http.put<Responses>(this.apiUrl + this.complement, user);
  }

  public deleteUser(id: string): Observable<Responses> {
    return this._http.delete<Responses>(this.apiUrl + this.complement, { body: { id } });
  }
}
