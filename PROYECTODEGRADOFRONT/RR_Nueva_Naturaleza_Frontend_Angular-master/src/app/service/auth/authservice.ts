import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, map, Subject } from 'rxjs';
import { Login } from '../../models/Login';
import { Responses } from '../../models/Response';
import { User } from '../../models/User';
import { UserPost } from '../../models/UserPost';
import { jwtDecode } from 'jwt-decode';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';
import { IForgetPassword } from '../../models/IForgetPassword';

@Injectable({
  providedIn: 'root'
})
export class Authservice {
  
  private readonly apiUrl = environment.endPoint;
  private complement = "Api/ApiUser/Login";
  private complement1 = "Api/ApiUser";

  public get userData(): User | null {
    return this.userSubject.value;
  }

  private userSubject = new BehaviorSubject<User | null>(null);
  public useer!: Observable<User | null>;

  constructor(private _http: HttpClient, private router: Router) {

    const storedUser = localStorage.getItem('Usuario');
    if (storedUser !== null) {
      const parsedUser = JSON.parse(storedUser);
      if (parsedUser) {
        this.userSubject = new BehaviorSubject<User | null>(parsedUser);
      }
    }
    this.useer = this.userSubject.asObservable();
  }

  login(login: Login): Observable<Responses> {
    console.log(this.apiUrl);

    return this._http.post<Responses>(this.apiUrl + this.complement, login).pipe(
      map(res => {
        if (res.result == 0) {
          console.log(res.data);
          const user: User = res.data;
          localStorage.setItem('Usuario', JSON.stringify(user));
          this.userSubject.next(user);
          console.log(user);
        }
        return res;
      })
    );
  }

  recuperar(recuperar: IForgetPassword): Observable<Responses> {    
    return this._http.post<Responses>(this.apiUrl + "Api/ApiUser/ForgetPassword", recuperar)
  }

  getTokenUserInfo(): UserPost | null {

    const token = localStorage.getItem('Usuario');

    if (token) {
      const user = jwtDecode(token) as UserPost;
      console.log(user.nameid); // Acceso al ID del usuario
      console.log(user.name); // Acceso al nombre del usuario
      console.log(user.rolId); // Acceso al ID del rol del usuario
      return user;
    }

    return null;

  }

  logout() {
    localStorage.removeItem('Usuario');
    this.userSubject.next(null);
    this.router.navigate(['']);
  }

}
